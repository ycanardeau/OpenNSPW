namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// A device paired with the computer.
/// </summary>
/// <param name="EndPoint">Its address.</param>
/// <param name="Name">Its name, or its address if it has none.</param>
public sealed record BluetoothDevice(BluetoothEndPoint EndPoint, string Name);
