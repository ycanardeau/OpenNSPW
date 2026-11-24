// A pooled mailbox's handler goes idle after one message as the next is posted: the next is handled, without waiting
// for another post. The scenario that VOLATILE_UNMARK fails.
#include "mailbox.h"

static struct mailbox mailbox;
static int handled;

static void receive(int message)
{
	// Plain: GenMC reports a race if two runs of the handler could overlap, or one not see what the one before did.
	assert(message == handled + 1);
	handled++;
}

static void *poster(void *arg)
{
	mailbox_post(&mailbox, 1);
	mailbox_post(&mailbox, 2);
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1000, receive);
	mailbox_start(&mailbox);
	pthread_t thread;
	pthread_create(&thread, 0, poster, 0);
	pthread_join(thread, 0);
	mailbox_join_runs(&mailbox);
	assert(handled == 2);
	return 0;
}
