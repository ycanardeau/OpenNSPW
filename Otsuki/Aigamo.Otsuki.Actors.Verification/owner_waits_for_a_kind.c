// An owner waits for one kind of message, the odd ones, as a poster of each kind races it: another kind may wake it,
// and it sleeps again; it takes the kind it waits for, and finds the other after.
#include "mailbox.h"

static struct mailbox mailbox;

static bool is_odd(int message)
{
	return message % 2 == 1;
}

static void *even_poster(void *arg)
{
	mailbox_post(&mailbox, 0);
	return 0;
}

static void *odd_poster(void *arg)
{
	mailbox_post(&mailbox, 1);
	return 0;
}

static void *owner(void *arg)
{
	int message;
	assert(mailbox_wait_for(&mailbox, is_odd));
	assert(mailbox_try_take(&mailbox, is_odd, true, &message) && message == 1);
	assert(mailbox_wait_for(&mailbox, 0));
	assert(mailbox_try_receive(&mailbox, &message) && message == 0);
	assert(!mailbox_try_receive(&mailbox, &message));
	return 0;
}

int main(void)
{
	mailbox_init(&mailbox, 1000, 0);
	pthread_t owning, evens, odds;
	pthread_create(&owning, 0, owner, 0);
	pthread_create(&evens, 0, even_poster, 0);
	pthread_create(&odds, 0, odd_poster, 0);
	pthread_join(evens, 0);
	pthread_join(odds, 0);
	pthread_join(owning, 0);
	return 0;
}
