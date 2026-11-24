// An owner with a thread of its own sleeps whenever its mailbox is empty, as posters race it: it wakes for each
// message, and receives each poster's in its order. A message left in the queue would leave it asleep for good, which
// GenMC reports with --check-liveness. The scenario that VOLATILE_UNMARK fails.
#include "mailbox.h"

#define POSTERS 2
#define MESSAGES 2

static struct mailbox mailbox;

static void *poster(void *arg)
{
	int id = (int)(long)arg;
	for (int i = 0; i < MESSAGES / POSTERS + (id < MESSAGES % POSTERS); i++)
		mailbox_post(&mailbox, id * 10 + i);
	return 0;
}

static void *owner(void *arg)
{
	int next[POSTERS] = {0};
	int received = 0;
	while (received < MESSAGES) {
		assert(mailbox_wait_for(&mailbox, 0));
		int message;
		while (mailbox_try_receive(&mailbox, &message)) {
			assert(message % 10 == next[message / 10]);
			next[message / 10]++;
			received++;
		}
	}
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1000, 0);
	pthread_t owning, posters[POSTERS];
	pthread_create(&owning, 0, owner, 0);
	for (int i = 0; i < POSTERS; i++)
		pthread_create(&posters[i], 0, poster, (void *)(long)i);
	for (int i = 0; i < POSTERS; i++)
		pthread_join(posters[i], 0);
	pthread_join(owning, 0);
	return 0;
}
