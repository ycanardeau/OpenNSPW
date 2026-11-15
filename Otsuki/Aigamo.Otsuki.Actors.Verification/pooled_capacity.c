// A bounded mailbox drops what is posted beyond its capacity, and handles all that it took.
#include "mailbox.h"

static struct mailbox mailbox;
static int handled;
static atomic_int accepted;

static void receive(int message)
{
	handled++;
}

static void *poster(void *arg)
{
	if (mailbox_post(&mailbox, 0))
		atomic_fetch_add_explicit(&accepted, 1, RLX);
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1, receive);
	mailbox_start(&mailbox);
	pthread_t posters[2];
	pthread_create(&posters[0], 0, poster, 0);
	pthread_create(&posters[1], 0, poster, 0);
	pthread_join(posters[0], 0);
	pthread_join(posters[1], 0);
	mailbox_join_runs(&mailbox);
	int taken = atomic_load_explicit(&accepted, RLX);
	assert(taken >= 1);
	assert(handled == taken);
	return 0;
}
