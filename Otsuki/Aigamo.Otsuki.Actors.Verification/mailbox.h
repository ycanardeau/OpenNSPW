// A model of Mailbox<T> (../Aigamo.Otsuki.Actors/Mailbox.cs) for GenMC, which runs each scenario that includes it in
// every order that its threads' steps can take, under a memory model that says which writes each read may see
// (README.md).
//
// Mailbox.cs sha256: f02d4195206de4c986965268ad0d2118290ed5ff78700a73b2cbcbbe5330430e
//
// The hash is that of Mailbox.cs as this model follows it, with its lines ended by LF: a test fails when Mailbox.cs
// changes and the hash does not (MailboxModelTests), so that the model is brought along with the code it checks.
//
// Each step of the C# is a step here, with what .NET promises of it, or less:
// - an Interlocked method is a full fence on every architecture: a seq_cst read-modify-write between two seq_cst
//   fences (IL_*);
// - Volatile.Read and Volatile.Write, as ConcurrentQueue<T> uses them, are an acquire load and a release store;
// - what one thread only touches (_passed, a slot's item between its sequence number's release and acquire) is plain,
//   and GenMC reports a race on it if two threads could touch it at once;
// - SemaphoreSlim, which a lock guards, is an acquire on Wait and a release on Release.
// Less than .NET promises is a model in which more can go wrong: what is right in it is right in .NET.
//
// With VOLATILE_UNMARK, the owner sets its wake word back to 0 with a release store, as Mailbox.cs did with
// Volatile.Write before commit 2e6a32f: the scenarios find the message that it left.

#include <assert.h>
#include <pthread.h>
#include <stdatomic.h>
#include <stdbool.h>

#define RLX memory_order_relaxed
#define ACQ memory_order_acquire
#define REL memory_order_release
#define SC memory_order_seq_cst

// Interlocked: a full fence, on either side of the operation.
#define FENCE() atomic_thread_fence(SC)

static inline long il_read(atomic_long *p)
{
	FENCE();
	long v = atomic_fetch_add_explicit(p, 0, SC);
	FENCE();
	return v;
}

static inline long il_add(atomic_long *p, long n)
{
	FENCE();
	long v = atomic_fetch_add_explicit(p, n, SC);
	FENCE();
	return v + n;
}

static inline long il_or(atomic_long *p, long n)
{
	FENCE();
	long v = atomic_fetch_or_explicit(p, n, SC);
	FENCE();
	return v;
}

// Interlocked.CompareExchange: the value it found.
static inline long il_cas_long(atomic_long *p, long value, long comparand)
{
	FENCE();
	atomic_compare_exchange_strong_explicit(p, &comparand, value, SC, SC);
	FENCE();
	return comparand;
}

static inline int il_cas(atomic_int *p, int value, int comparand)
{
	FENCE();
	atomic_compare_exchange_strong_explicit(p, &comparand, value, SC, SC);
	FENCE();
	return comparand;
}

static inline void il_exchange(atomic_int *p, int value)
{
	FENCE();
	atomic_exchange_explicit(p, value, SC);
	FENCE();
}

// ConcurrentQueue<T>: one segment of it (ConcurrentQueueSegment<T>), which the scenarios never fill. Each slot has a
// sequence number, which says whose turn it is: an enqueuer's at the slot's index, a dequeuer's one after.

#ifndef QUEUE_SLOTS
#define QUEUE_SLOTS 4
#endif

struct slot {
	atomic_int sequence;
	int item;
};

struct queue {
	struct slot slots[QUEUE_SLOTS];
	atomic_int head;
	atomic_int tail;
};

static void queue_init(struct queue *q)
{
	for (int i = 0; i < QUEUE_SLOTS; i++)
		atomic_init(&q->slots[i].sequence, i);
	atomic_init(&q->head, 0);
	atomic_init(&q->tail, 0);
}

static void queue_enqueue(struct queue *q, int item)
{
	while (true) {
		int tail = atomic_load_explicit(&q->tail, ACQ);
		struct slot *slot = &q->slots[tail % QUEUE_SLOTS];
		int sequence = atomic_load_explicit(&slot->sequence, ACQ);
		assert(sequence - tail >= 0); // The scenarios never fill the segment.
		if (sequence == tail) {
			atomic_int *word = &q->tail;
			if (il_cas(word, tail + 1, tail) == tail) {
				slot->item = item;
				atomic_store_explicit(&slot->sequence, tail + 1, REL);
				return;
			}
		}
	}
}

// TryDequeue (remove) and TryPeek's emptiness (IsEmpty): false only if no enqueue has taken a slot; while one has, and
// has not yet written its item, it waits, as ConcurrentQueue<T> spins.
static bool queue_take(struct queue *q, bool remove, int *item)
{
	while (true) {
		int head = atomic_load_explicit(&q->head, ACQ);
		struct slot *slot = &q->slots[head % QUEUE_SLOTS];
		int sequence = atomic_load_explicit(&slot->sequence, ACQ);
		if (sequence == head + 1) {
			if (!remove)
				return true;
			if (il_cas(&q->head, head + 1, head) == head) {
				*item = slot->item;
				atomic_store_explicit(&slot->sequence, head + QUEUE_SLOTS, REL);
				return true;
			}
		} else if (sequence - (head + 1) < 0) {
			int tail = atomic_load_explicit(&q->tail, ACQ);
			if (tail - head <= 0)
				return false;
		}
	}
}

static bool queue_is_empty(struct queue *q)
{
	int unused;
	return !queue_take(q, false, &unused);
}

// SemaphoreSlim(0): Wait sleeps until a Release, and takes it.
static void semaphore_release(atomic_int *count)
{
	atomic_fetch_add_explicit(count, 1, REL);
}

static void semaphore_wait(atomic_int *count)
{
	while (true) {
		int seen = atomic_load_explicit(count, ACQ);
		if (seen > 0 && atomic_compare_exchange_strong_explicit(count, &seen, seen - 1, ACQ, RLX))
			return;
	}
}

// The mailbox.

#define COMPLETED 1L
#define ONE_MESSAGE 2L

#ifndef MAX_RUNS
#define MAX_RUNS 8
#endif

#ifndef MAX_PASSED
#define MAX_PASSED 8
#endif

struct mailbox {
	struct queue queue;
	long capacity;
	bool pooled;
	void (*receive)(int message);

	atomic_long state;
	atomic_int is_signaled;
	atomic_int is_scheduled;
	atomic_int signal;

	// TaskCompletionSource: set once, read once every thread is joined.
	atomic_int completion;

	// The owner's only.
	int passed[MAX_PASSED];
	int passed_count;

	// The thread pool: each Schedule a thread of its own, which the scenario joins at its end.
	pthread_t runs[MAX_RUNS];
	atomic_int run_count;
};

static void mailbox_init(struct mailbox *m, long capacity, void (*receive)(int message))
{
	queue_init(&m->queue);
	m->capacity = capacity;
	m->pooled = receive != 0;
	m->receive = receive;
	atomic_init(&m->state, 0);
	atomic_init(&m->is_signaled, 0);
	// A pooled mailbox is held until Start (the constructor's _isScheduled = 1).
	atomic_init(&m->is_scheduled, m->pooled ? 1 : 0);
	atomic_init(&m->signal, 0);
	atomic_init(&m->completion, 0);
	m->passed_count = 0;
	atomic_init(&m->run_count, 0);
}

// The owner sets its wake word back to 0, before it looks at the queue again.
static void unmark(atomic_int *word)
{
#ifdef VOLATILE_UNMARK
	atomic_store_explicit(word, 0, REL);
#else
	il_exchange(word, 0);
#endif
}

static void mailbox_signal(struct mailbox *m)
{
	if (il_cas(&m->is_signaled, 1, 0) == 0)
		semaphore_release(&m->signal);
}

static bool mailbox_try_dequeue(struct mailbox *m, int *message)
{
	if (!queue_take(&m->queue, true, message))
		return false;

	il_add(&m->state, -ONE_MESSAGE);
	return true;
}

static void mailbox_run(struct mailbox *m)
{
	while (true) {
		int message;
		while (mailbox_try_dequeue(m, &message))
			m->receive(message);

		if (il_read(&m->state) == COMPLETED) {
			atomic_store_explicit(&m->completion, 1, REL);
			return;
		}

		unmark(&m->is_scheduled);
		if ((queue_is_empty(&m->queue) && il_read(&m->state) != COMPLETED)
		    || il_cas(&m->is_scheduled, 1, 0) != 0)
			return;
	}
}

static void *mailbox_run_thread(void *arg)
{
	mailbox_run((struct mailbox *)arg);
	return 0;
}

static void mailbox_schedule(struct mailbox *m)
{
	if (il_cas(&m->is_scheduled, 1, 0) == 0) {
		int index = atomic_fetch_add_explicit(&m->run_count, 1, RLX);
		assert(index < MAX_RUNS);
		pthread_create(&m->runs[index], 0, mailbox_run_thread, m);
	}
}

static void mailbox_notify(struct mailbox *m)
{
	if (m->pooled)
		mailbox_schedule(m);
	else
		mailbox_signal(m);
}

static bool mailbox_post(struct mailbox *m, int message)
{
	long state = il_read(&m->state);
	while (true) {
		if ((state & COMPLETED) != 0 || state / ONE_MESSAGE >= m->capacity)
			return false;

		long seen = il_cas_long(&m->state, state + ONE_MESSAGE, state);
		if (seen == state)
			break;

		state = seen;
	}

	queue_enqueue(&m->queue, message);
	mailbox_notify(m);
	return true;
}

static void mailbox_complete(struct mailbox *m)
{
	if ((il_or(&m->state, COMPLETED) & COMPLETED) == 0)
		mailbox_notify(m);
}

static void mailbox_start(struct mailbox *m)
{
	unmark(&m->is_scheduled);
	if (!queue_is_empty(&m->queue) || il_read(&m->state) == COMPLETED)
		mailbox_schedule(m);
}

// Joins the thread pool's runs, once every thread that schedules them is joined.
static void mailbox_join_runs(struct mailbox *m)
{
	int count = atomic_load_explicit(&m->run_count, RLX);
	for (int i = 0; i < count; i++)
		pthread_join(m->runs[i], 0);
}

// The owner's selective receive (TryTake): the first message that matches (any, if matches is 0), taken out or only
// looked at, first among those passed over, then from the queue, passing over those that do not match.
static bool mailbox_try_take(struct mailbox *m, bool (*matches)(int), bool remove, int *message)
{
	for (int i = 0; i < m->passed_count; i++) {
		if (matches == 0 || matches(m->passed[i])) {
			*message = m->passed[i];
			if (remove) {
				for (int j = i + 1; j < m->passed_count; j++)
					m->passed[j - 1] = m->passed[j];
				m->passed_count--;
			}
			return true;
		}
	}

	int next;
	while (mailbox_try_dequeue(m, &next)) {
		if (matches == 0 || matches(next)) {
			if (!remove) {
				assert(m->passed_count < MAX_PASSED);
				m->passed[m->passed_count++] = next;
			}
			*message = next;
			return true;
		}

		assert(m->passed_count < MAX_PASSED);
		m->passed[m->passed_count++] = next;
	}

	return false;
}

static bool mailbox_try_receive(struct mailbox *m, int *message)
{
	return mailbox_try_take(m, 0, true, message);
}

// WaitFor with no timeout, and WaitToReceiveAsync, which is WaitFor of any message.
static bool mailbox_wait_for(struct mailbox *m, bool (*matches)(int))
{
	int unused;
	while (!mailbox_try_take(m, matches, false, &unused)) {
		if (il_read(&m->state) == COMPLETED)
			return false;

		semaphore_wait(&m->signal);
		unmark(&m->is_signaled);
	}

	return true;
}
