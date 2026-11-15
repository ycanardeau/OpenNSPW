// Two posters race each other and a third thread that completes the mailbox: each message posted before it completed
// is handled, and the mailbox's completion is set once they are, and only then.
#include "mailbox.h"

static struct mailbox mailbox;
static int handled;
static atomic_int accepted;

static void receive(int message)
{
	handled++;
	assert(atomic_load_explicit(&mailbox.completion, RLX) == 0);
}

static void *poster(void *arg)
{
	if (mailbox_post(&mailbox, (int)(long)arg))
		atomic_fetch_add_explicit(&accepted, 1, RLX);
	return 0;
}

static void *completer(void *arg)
{
	mailbox_complete(&mailbox);
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1000, receive);
	mailbox_start(&mailbox);
	pthread_t posters[2], completing;
	pthread_create(&posters[0], 0, poster, (void *)0L);
	pthread_create(&posters[1], 0, poster, (void *)1L);
	pthread_create(&completing, 0, completer, 0);
	pthread_join(posters[0], 0);
	pthread_join(posters[1], 0);
	pthread_join(completing, 0);
	mailbox_join_runs(&mailbox);
	assert(handled == atomic_load_explicit(&accepted, RLX));
	assert(atomic_load_explicit(&mailbox.completion, RLX) == 1);
	return 0;
}
