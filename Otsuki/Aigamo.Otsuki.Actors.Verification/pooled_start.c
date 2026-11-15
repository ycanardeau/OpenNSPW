// A pooled mailbox holds what is posted until it starts, and handles it all once it has: posts race the start.
#include "mailbox.h"

static struct mailbox mailbox;
static int handled;

static void receive(int message)
{
	assert(message == handled + 1);
	handled++;
}

static void *poster(void *arg)
{
	mailbox_post(&mailbox, 1);
	mailbox_post(&mailbox, 2);
	return 0;
}

static void *starter(void *arg)
{
	mailbox_start(&mailbox);
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1000, receive);
	pthread_t posting, starting;
	pthread_create(&posting, 0, poster, 0);
	pthread_create(&starting, 0, starter, 0);
	pthread_join(posting, 0);
	pthread_join(starting, 0);
	mailbox_join_runs(&mailbox);
	assert(handled == 2);
	return 0;
}
