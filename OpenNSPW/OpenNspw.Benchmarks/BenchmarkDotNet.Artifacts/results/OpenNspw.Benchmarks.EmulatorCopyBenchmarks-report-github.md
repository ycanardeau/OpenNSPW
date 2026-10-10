```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M2, 1 CPU, 8 logical and 8 physical cores
.NET SDK 11.0.100-preview.7.26381.103
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method  | Mean     | Error    | StdDev   | Allocated |
|-------- |---------:|---------:|---------:|----------:|
| CopyIn  | 10.42 μs | 0.020 μs | 0.016 μs |         - |
| CopyOut | 10.59 μs | 0.053 μs | 0.047 μs |         - |
