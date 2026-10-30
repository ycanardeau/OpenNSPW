// A C interface to IOBluetooth's RFCOMM channels, for MacBluetoothConnector. IOBluetooth delivers its callbacks on the
// run loop of the thread that asked for them, so every call runs on a thread of the host's own, which runs a run loop,
// and every callback comes from that thread.

#import <Foundation/Foundation.h>
#import <IOBluetooth/IOBluetooth.h>

// Whether to log each step to standard error, to find where a channel stops: when OTSUKI_BLUETOOTH_LOG is 1.
static BOOL OtsukiBluetoothLogging(void)
{
	static BOOL logging;
	static dispatch_once_t once;
	dispatch_once(&once, ^{
		logging = [NSProcessInfo.processInfo.environment[@"OTSUKI_BLUETOOTH_LOG"] isEqualToString:@"1"];
	});
	return logging;
}

#define OTSUKI_LOG(...) do { if (OtsukiBluetoothLogging()) NSLog(@"Otsuki Bluetooth: " __VA_ARGS__); } while (0)

typedef struct
{
	// A remote device opened a channel to the published service.
	void (*accepted)(void *context, int64_t channel, const uint8_t *address);
	// A channel that otsuki_bluetooth_connect asked for opened, or failed to with channel 0.
	void (*opened)(void *context, int64_t request, int64_t channel, int32_t status);
	void (*received)(void *context, int64_t channel, const void *data, int32_t length);
	// The remote device closed the channel, or it was lost. Not called for otsuki_bluetooth_close.
	void (*closed)(void *context, int64_t channel);
} OtsukiBluetoothCallbacks;

@interface OtsukiBluetoothHost : NSObject <IOBluetoothRFCOMMChannelDelegate>
@end

@implementation OtsukiBluetoothHost
{
	OtsukiBluetoothCallbacks _callbacks;
	void *_context;
	NSThread *_thread;
	BOOL _stopped;
	int64_t _nextChannel;
	NSMutableDictionary<NSNumber *, IOBluetoothRFCOMMChannel *> *_channels;
	NSMapTable<IOBluetoothRFCOMMChannel *, NSNumber *> *_channelIds;
	// Channels that are opening, with the request that asked for each.
	NSMapTable<IOBluetoothRFCOMMChannel *, NSNumber *> *_opening;
	// SDP queries under way, by device address: the requests waiting for each, with the service they look for.
	NSMutableDictionary<NSData *, NSMutableArray<NSArray *> *> *_queries;
	IOBluetoothSDPServiceRecord *_record;
	IOBluetoothUserNotification *_notification;
}

- (void)runLoop
{
	@autoreleasepool
	{
		NSRunLoop *runLoop = NSRunLoop.currentRunLoop;
		OTSUKI_LOG(@"run loop started");
		// A run loop without sources returns at once; the port keeps it waiting for performed selectors.
		[runLoop addPort:NSMachPort.port forMode:NSDefaultRunLoopMode];
		while (!_stopped)
		{
			@autoreleasepool
			{
				[runLoop runMode:NSDefaultRunLoopMode beforeDate:NSDate.distantFuture];
			}
		}
	}
}

- (instancetype)initWithCallbacks:(OtsukiBluetoothCallbacks)callbacks context:(void *)context
{
	if ((self = [super init]))
	{
		_callbacks = callbacks;
		_context = context;
		_channels = [NSMutableDictionary dictionary];
		NSPointerFunctionsOptions identity =
			NSPointerFunctionsStrongMemory | NSPointerFunctionsObjectPointerPersonality;
		_channelIds = [NSMapTable mapTableWithKeyOptions:identity valueOptions:NSPointerFunctionsStrongMemory];
		_opening = [NSMapTable mapTableWithKeyOptions:identity valueOptions:NSPointerFunctionsStrongMemory];
		_queries = [NSMutableDictionary dictionary];
		_thread = [[NSThread alloc] initWithTarget:self selector:@selector(runLoop) object:nil];
		_thread.name = @"Otsuki Bluetooth";
		[_thread start];
	}
	return self;
}

- (void)invoke:(dispatch_block_t)block
{
	block();
}

- (void)perform:(dispatch_block_t)block wait:(BOOL)wait
{
	[self performSelector:@selector(invoke:) onThread:_thread withObject:[block copy] waitUntilDone:wait];
}

- (int64_t)addChannel:(IOBluetoothRFCOMMChannel *)channel
{
	NSNumber *channelId = @(++_nextChannel);
	_channels[channelId] = channel;
	[_channelIds setObject:channelId forKey:channel];
	return channelId.longLongValue;
}

- (void)removeChannel:(IOBluetoothRFCOMMChannel *)channel
{
	NSNumber *channelId = [_channelIds objectForKey:channel];
	if (channelId != nil)
		[_channels removeObjectForKey:channelId];
	[_channelIds removeObjectForKey:channel];
}

- (void)channelOpened:(IOBluetoothUserNotification *)notification channel:(IOBluetoothRFCOMMChannel *)channel
{
	if (_stopped)
		return;

	int64_t channelId = [self addChannel:channel];
	OTSUKI_LOG(@"%@ opened channel %u as %lld", [[channel getDevice] addressString], (unsigned)[channel getChannelID],
	           (long long)channelId);
	[channel setDelegate:self];
	_callbacks.accepted(_context, channelId, [[channel getDevice] getAddress]->data);
}

- (IOReturn)listenOnThread:(NSData *)service name:(NSString *)name
{
	if (_record != nil)
		return kIOReturnExclusiveAccess;

	// Channel 1 is a placeholder: the system assigns a free channel when 1 is in use.
	NSDictionary *channel = @{@"DataElementType" : @1, @"DataElementSize" : @1, @"DataElementValue" : @1};
	NSDictionary *record = @{
		@"0001 - ServiceClassIDList" : @[ [IOBluetoothSDPUUID uuidWithData:service] ],
		@"0004 - ProtocolDescriptorList" : @[
			@[ [IOBluetoothSDPUUID uuid16:kBluetoothSDPUUID16L2CAP] ],
			@[ [IOBluetoothSDPUUID uuid16:kBluetoothSDPUUID16RFCOMM], channel ],
		],
		@"0005 - BrowseGroupList" : @[ [IOBluetoothSDPUUID uuid16:kBluetoothSDPUUID16ServiceClassPublicBrowseGroup] ],
		@"0100 - ServiceName" : name,
	};
	_record = [IOBluetoothSDPServiceRecord publishedServiceRecordWithDictionary:record];
	if (_record == nil)
	{
		OTSUKI_LOG(@"could not publish the service %@", name);
		return kIOReturnError;
	}

	BluetoothRFCOMMChannelID channelId;
	IOReturn status = [_record getRFCOMMChannelID:&channelId];
	if (status != kIOReturnSuccess)
	{
		OTSUKI_LOG(@"published the service %@ without a channel (0x%08X)", name, status);
		[_record removeServiceRecord];
		_record = nil;
		return status;
	}

	_notification = [IOBluetoothRFCOMMChannel registerForChannelOpenNotifications:self
	                                                                     selector:@selector(channelOpened:channel:)
	                                                                withChannelID:channelId
	                                                                    direction:kIOBluetoothUserNotificationChannelDirectionIncoming];
	OTSUKI_LOG(@"published the service %@ on channel %u, %@", name, (unsigned)channelId,
	           _notification == nil ? @"but cannot be told of channels" : @"waiting for channels");
	return kIOReturnSuccess;
}

- (IOReturn)listen:(NSData *)service name:(NSString *)name
{
	__block IOReturn status;
	[self perform:^{
		status = [self listenOnThread:service name:name];
	}
	         wait:YES];
	return status;
}

- (void)openChannelTo:(IOBluetoothDevice *)device request:(NSNumber *)request service:(NSData *)service
{
	IOBluetoothSDPServiceRecord *record = [device getServiceRecordForUUID:[IOBluetoothSDPUUID uuidWithData:service]];
	BluetoothRFCOMMChannelID channelId;
	IOReturn status = record == nil ? kIOReturnNotFound : [record getRFCOMMChannelID:&channelId];
	IOBluetoothRFCOMMChannel *channel = nil;
	if (status == kIOReturnSuccess)
		status = [device openRFCOMMChannelAsync:&channel withChannelID:channelId delegate:self];
	OTSUKI_LOG(@"%@ %@ the service; opening its channel: 0x%08X", device.addressString,
	           record == nil ? @"does not have" : @"has", status);
	if (status == kIOReturnSuccess && channel != nil)
		[_opening setObject:request forKey:channel];
	else
		_callbacks.opened(_context, request.longLongValue, 0, status == kIOReturnSuccess ? kIOReturnError : status);
}

- (void)sdpQueryComplete:(IOBluetoothDevice *)device status:(IOReturn)status
{
	NSData *address = [NSData dataWithBytes:[device getAddress] length:sizeof(BluetoothDeviceAddress)];
	NSMutableArray<NSArray *> *waiting = _queries[address];
	OTSUKI_LOG(@"SDP query of %@ completed: 0x%08X", device.addressString, status);
	[_queries removeObjectForKey:address];
	for (NSArray *entry in waiting)
	{
		if (status == kIOReturnSuccess)
			[self openChannelTo:device request:entry[0] service:entry[1]];
		else
			_callbacks.opened(_context, [entry[0] longLongValue], 0, status);
	}
}

- (void)connectOnThread:(NSData *)address request:(NSNumber *)request service:(NSData *)service
{
	if (_stopped)
		return;

	NSMutableArray<NSArray *> *waiting = _queries[address];
	if (waiting != nil)
	{
		[waiting addObject:@[ request, service ]];
		return;
	}

	IOBluetoothDevice *device = [IOBluetoothDevice deviceWithAddress:address.bytes];
	// The channel of the service can change, so it is looked up every time.
	IOReturn status = device == nil
		? kIOReturnNotFound
		: [device performSDPQuery:self uuids:@[ [IOBluetoothSDPUUID uuidWithData:service] ]];
	OTSUKI_LOG(@"querying SDP of %@: 0x%08X", device.addressString, status);
	if (status == kIOReturnSuccess)
		_queries[address] = [NSMutableArray arrayWithObject:@[ request, service ]];
	else
		_callbacks.opened(_context, request.longLongValue, 0, status);
}

- (void)connect:(NSData *)address request:(int64_t)request service:(NSData *)service
{
	[self perform:^{
		[self connectOnThread:address request:@(request) service:service];
	}
	         wait:NO];
}

- (void)write:(NSData *)data channel:(int64_t)channelId
{
	[self perform:^{
		IOBluetoothRFCOMMChannel *channel = self->_channels[@(channelId)];
		if (channel == nil)
			return;

		NSUInteger mtu = MAX([channel getMTU], 1);
		for (NSUInteger offset = 0; offset < data.length; offset += mtu)
		{
			// The chunk must live until the write completes, so the refcon holds it.
			NSData *chunk = [data subdataWithRange:NSMakeRange(offset, MIN(mtu, data.length - offset))];
			void *refcon = (__bridge_retained void *)chunk;
			IOReturn status = [channel writeAsync:(void *)chunk.bytes length:(UInt16)chunk.length refcon:refcon];
			if (status != kIOReturnSuccess)
			{
				OTSUKI_LOG(@"could not write %lu bytes to %lld: 0x%08X", (unsigned long)chunk.length, channelId,
				           status);
				// Nothing was sent. The rest is lost too, so that the stream does not skip bytes.
				CFBridgingRelease(refcon);
				break;
			}
		}
	}
	         wait:NO];
}

- (void)closeOnThread:(IOBluetoothRFCOMMChannel *)channel
{
	[self removeChannel:channel];
	[channel setDelegate:nil];
	[channel closeChannel];
}

- (void)close:(int64_t)channelId
{
	[self perform:^{
		IOBluetoothRFCOMMChannel *channel = self->_channels[@(channelId)];
		if (channel != nil)
			[self closeOnThread:channel];
	}
	         wait:NO];
}

- (void)stop
{
	[self perform:^{
		self->_stopped = YES;
		[self->_notification unregister];
		self->_notification = nil;
		[self->_record removeServiceRecord];
		self->_record = nil;
		for (IOBluetoothRFCOMMChannel *channel in self->_channels.allValues)
			[self closeOnThread:channel];
		for (IOBluetoothRFCOMMChannel *channel in self->_opening.keyEnumerator.allObjects)
			[self closeOnThread:channel];
		[self->_opening removeAllObjects];
		[self->_queries removeAllObjects];
	}
	         wait:YES];
}

- (void)rfcommChannelOpenComplete:(IOBluetoothRFCOMMChannel *)channel status:(IOReturn)error
{
	NSNumber *request = [_opening objectForKey:channel];
	if (request == nil)
		return;

	[_opening removeObjectForKey:channel];
	OTSUKI_LOG(@"opened channel %u of %@: 0x%08X", (unsigned)[channel getChannelID], [[channel getDevice] addressString],
	           error);
	if (error != kIOReturnSuccess)
	{
		[channel setDelegate:nil];
		_callbacks.opened(_context, request.longLongValue, 0, error);
		return;
	}

	_callbacks.opened(_context, request.longLongValue, [self addChannel:channel], kIOReturnSuccess);
}

- (void)rfcommChannelData:(IOBluetoothRFCOMMChannel *)channel data:(void *)data length:(size_t)length
{
	NSNumber *channelId = [_channelIds objectForKey:channel];
	OTSUKI_LOG(@"received %zu bytes on %@", length, (id)channelId ?: @"a channel it does not know");
	if (channelId != nil)
		_callbacks.received(_context, channelId.longLongValue, data, (int32_t)length);
}

- (void)rfcommChannelClosed:(IOBluetoothRFCOMMChannel *)channel
{
	OTSUKI_LOG(@"channel %u of %@ closed", (unsigned)[channel getChannelID], [[channel getDevice] addressString]);
	NSNumber *request = [_opening objectForKey:channel];
	if (request != nil)
	{
		[_opening removeObjectForKey:channel];
		_callbacks.opened(_context, request.longLongValue, 0, kIOReturnNotOpen);
		return;
	}

	NSNumber *channelId = [_channelIds objectForKey:channel];
	if (channelId == nil)
		return;

	[self removeChannel:channel];
	_callbacks.closed(_context, channelId.longLongValue);
}

- (void)rfcommChannelWriteComplete:(IOBluetoothRFCOMMChannel *)channel refcon:(void *)refcon status:(IOReturn)error
{
	if (refcon != NULL)
		CFBridgingRelease(refcon);
}

@end

// The C interface. Addresses are the 6 bytes of a BluetoothDeviceAddress and services are 16-byte UUIDs, both in the
// order they are written in.

void *otsuki_bluetooth_create(OtsukiBluetoothCallbacks callbacks, void *context)
{
	return (__bridge_retained void *)[[OtsukiBluetoothHost alloc] initWithCallbacks:callbacks context:context];
}

// Stops the host. No callback comes after it returns.
void otsuki_bluetooth_destroy(void *host)
{
	OtsukiBluetoothHost *bluetooth = (__bridge_transfer OtsukiBluetoothHost *)host;
	[bluetooth stop];
}

// Whether the app declares why it uses Bluetooth (NSBluetoothAlwaysUsageDescription in its Info.plist). Without it,
// macOS kills the process at its first Bluetooth call. This touches no Bluetooth.
int32_t otsuki_bluetooth_usage_described(void)
{
	return [NSBundle.mainBundle objectForInfoDictionaryKey:@"NSBluetoothAlwaysUsageDescription"] != nil;
}

// Calls back with the address and the name of each device paired with the computer.
void otsuki_bluetooth_paired_devices(void (*device)(void *context, const uint8_t *address, const char *name),
                                     void *context)
{
	for (IOBluetoothDevice *paired in IOBluetoothDevice.pairedDevices)
		device(context, [paired getAddress]->data, (paired.nameOrAddress ?: @"").UTF8String);
}

// The address of the computer's Bluetooth controller. Returns an IOReturn.
int32_t otsuki_bluetooth_local_address(uint8_t *address)
{
	NSString *text = IOBluetoothHostController.defaultController.addressAsString;
	if (text == nil)
		return kIOReturnNoDevice;

	BluetoothDeviceAddress deviceAddress;
	IOReturn status = IOBluetoothNSStringToDeviceAddress(text, &deviceAddress);
	if (status == kIOReturnSuccess)
		memcpy(address, deviceAddress.data, sizeof deviceAddress.data);
	return status;
}

// Publishes the service, and accepts the channels that remote devices open to it. Returns an IOReturn.
int32_t otsuki_bluetooth_listen(void *host, const uint8_t *service, const char *name)
{
	return [(__bridge OtsukiBluetoothHost *)host listen:[NSData dataWithBytes:service length:16]
	                                               name:[NSString stringWithUTF8String:name]];
}

// Opens a channel to the service on a remote device. The opened callback reports the outcome with the request.
void otsuki_bluetooth_connect(void *host, int64_t request, const uint8_t *address, const uint8_t *service)
{
	[(__bridge OtsukiBluetoothHost *)host connect:[NSData dataWithBytes:address length:sizeof(BluetoothDeviceAddress)]
	                                      request:request
	                                      service:[NSData dataWithBytes:service length:16]];
}

// Sends data on a channel. The data is copied before this returns.
void otsuki_bluetooth_write(void *host, int64_t channel, const uint8_t *data, int32_t length)
{
	[(__bridge OtsukiBluetoothHost *)host write:[NSData dataWithBytes:data length:length] channel:channel];
}

void otsuki_bluetooth_close(void *host, int64_t channel)
{
	[(__bridge OtsukiBluetoothHost *)host close:channel];
}
