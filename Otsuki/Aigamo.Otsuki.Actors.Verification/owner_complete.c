// An owner receives until its mailbox is completed and empty, as a poster and a completer race it: it receives each
// message posted before the mailbox completed, and then stops.
#include "mailbox.h"

static struct mailbox mailbox;
static atomic_int accepted;

static void *poster(void *arg)
{
	for (int i = 0; i < 2; i++)
		if (mailbox_post(&mailbox, i))
			atomic_fetch_add_explicit(&accepted, 1, RLX);
	return 0;
}

static void *completer(void *arg)
{
	mailbox_complete(&mailbox);
	return 0;
}

static int received;

static void *owner(void *arg)
{
	while (mailbox_wait_for(&mailbox, 0)) {
		int message;
		while (mailbox_try_receive(&mailbox, &message)) {
			assert(message == received);
			received++;
		}
	}
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1000, 0);
	pthread_t owning, posting, completing;
	pthread_create(&owning, 0, owner, 0);
	pthread_create(&posting, 0, poster, 0);
	pthread_create(&completing, 0, completer, 0);
	pthread_join(posting, 0);
	pthread_join(completing, 0);
	pthread_join(owning, 0);
	assert(received == atomic_load_explicit(&accepted, RLX));
	return 0;
}
