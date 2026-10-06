# Code style

- Order definitions bottom-up (dependency-first), like a functional-programming source file where you can't reference something before it's bound: define a function/method/property only after everything it depends on is already defined above it. The entry point / top-level caller goes last, the leaves (depending on nothing else local) go first. Applies to properties and fields as well as functions and methods — order them by the same dependency rule (e.g. a computed property goes after the properties it reads).

    Example — correct order:

    ```
    void C() { }

    void B() { C(); }

    void A() { B(); }
    ```

    Incorrect (caller-first) order:

    ```
    void A() { B(); }

    void B() { C(); }

    void C() { }
    ```

    Apply this whenever touching a file: when writing new files, when adding new members to existing ones, and when editing an existing file whose members are out of order — reorder the existing members too so the whole file follows dependency order, not just the new additions.

- Use a primary constructor for a class or struct whenever its constructor only stores its parameters (possibly through a simple expression such as `errorText ?? string.Empty` or `value.ToImmutableArray()`) and/or passes them to the base constructor. Keep an explicit constructor when the body does anything else (validation, statements, calls, event subscriptions).

    - Never capture a primary constructor parameter. Store each one in an explicitly declared member through its initializer — a `private readonly` field when it is only used internally (`private readonly Connection _connection = connection;`), or a property when it is exposed (`public string Name { get; } = name;`) — and have every other member use that field or property, never the parameter. Parameters may only appear in member initializers and in the base-type argument list.

        Why: a captured parameter becomes a hidden, mutable field. It can't be `readonly`, so any method can reassign it; it reads like a local rather than instance state; and it can't carry field attributes such as `[GuardedBy]`.
    - Keep the existing members as they were (name, type, accessibility, accessors); only replace the constructor assignments with initializers.
    - Pass base-constructor arguments in the base list (`class Screen1(WindowManager windowManager) : ScreenBase(windowManager)`) instead of `: base(...)`.
    - Any additional constructors chain to the primary one with `: this(...)`.
    - Move the constructor's XML doc onto the type: its summary goes in `<remarks>` and its `<param>` tags stay as they are.

    Example — correct:

    ```
    internal sealed class ReliableChannel(Connection connection, Handshake handshake)
    {
        private readonly Connection _connection = connection;
        private readonly Handshake _handshake = handshake;
        private TimeSpan _roundTripTime = connection.Profile.InitialRoundTripTime;

        public void Send() => _connection.Send(/* ... */);
    }
    ```

    Incorrect (captures the parameter):

    ```
    internal sealed class ReliableChannel(Connection connection, Handshake handshake)
    {
        private TimeSpan _roundTripTime = connection.Profile.InitialRoundTripTime;

        public void Send() => connection.Send(/* ... */);
    }
    ```
