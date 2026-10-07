namespace OpenNspw.DirectPlay;

// IDirectPlay8Address: a service provider, a device, and components such as the host name and the port.
public sealed class DirectPlay8Address : IDirectPlay8Address
{
	private readonly Dictionary<string, object> _components = new(StringComparer.OrdinalIgnoreCase);

	public Guid? ServiceProvider { get; private set; }

	public Guid? Device { get; private set; }

	public string? Hostname => _components.GetValueOrDefault(dplay8.DPNA_KEY_HOSTNAME) as string;

	public int? Port => _components.GetValueOrDefault(dplay8.DPNA_KEY_PORT) is uint port ? (int)port : null;

	public int SetSP(Guid? pguidSP)
	{
		ServiceProvider = pguidSP;
		return winerror.S_OK;
	}

	public int SetDevice(Guid? devGuid)
	{
		Device = devGuid;
		return winerror.S_OK;
	}

	public int AddComponent(string pwszName, object pvData, uint dwDataSize, uint dwDataType)
	{
		_components[pwszName] = pvData;
		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}
}
