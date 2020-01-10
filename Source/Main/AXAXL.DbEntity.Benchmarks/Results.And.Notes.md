# Benchmark Results and Notes

Note that the database behind this benchmark is CLR.  The connection string can either be set in appsettings.json or in environment variable
**ConnectionString__CLR**.  For example, on Windows 10 machine, I have

```dos
SET ConnectionString__CLR=Server=sql-destiny-dev.r02.xlgs.local,1436; User Id=CLRMainDev; Password=devma1nclr; Database=xlre_clr_copy1
```

and on Linux box, I have

```bash
export ConnectionString__CLR='Server=sql-destiny-dev.r02.xlgs.local,1436; User Id=CLRMainDev; Password=devma1nclr; Database=xlre_clr_copy1'
```

## 2020-01-10

From Home

Async Build method

|                                                         Method |      Mean |    Error |   StdDev | Ratio | RatioSD |
|--------------------------------------------------------------- |----------:|---------:|---------:|------:|--------:|
|                                'Baseline. Query by direct SQL' |  19.51 ms | 0.387 ms | 0.414 ms |  1.00 |    0.00 |
|                               'Query by DbEntity Exec Command' |  41.60 ms | 0.685 ms | 0.640 ms |  2.13 |    0.05 |
|                        'Query by DbEntity with Optimization 2' | 329.94 ms | 4.542 ms | 3.546 ms | 16.86 |    0.34 |
|       'Query by DbEntity without Children with Optimization 2' |  25.23 ms | 0.502 ms | 1.357 ms |  1.27 |    0.08 |
|     'Query by DbEntity with only Mkt Loss with Optimization 2' | 182.54 ms | 4.144 ms | 7.258 ms |  9.43 |    0.45 |
| 'Query by DbEntity with only User Session with Optimization 2' | 180.08 ms | 2.214 ms | 1.963 ms |  9.21 |    0.28 |


Delegate was changed to be on static method and cached.

From Home

|                                                         Method |      Mean |     Error |    StdDev | Ratio | RatioSD |
|--------------------------------------------------------------- |----------:|----------:|----------:|------:|--------:|
|                                'Baseline. Query by direct SQL' |  19.46 ms |  0.258 ms |  0.242 ms |  1.00 |    0.00 |
|                               'Query by DbEntity Exec Command' |  42.57 ms |  0.673 ms |  0.629 ms |  2.19 |    0.04 |
|                        'Query by DbEntity with Optimization 2' | 367.53 ms | 10.947 ms | 31.233 ms | 18.56 |    1.13 |
|       'Query by DbEntity without Children with Optimization 2' |  27.58 ms |  0.824 ms |  2.428 ms |  1.32 |    0.15 |
|     'Query by DbEntity with only Mkt Loss with Optimization 2' | 207.12 ms |  4.740 ms | 13.677 ms | 10.50 |    0.59 |
| 'Query by DbEntity with only User Session with Optimization 2' | 208.09 ms |  5.737 ms | 16.460 ms | 10.73 |    0.43 |

From Office

|                                                         Method |     Mean |    Error |    StdDev | Ratio | RatioSD |
|--------------------------------------------------------------- |---------:|---------:|----------:|------:|--------:|
|                                'Baseline. Query by direct SQL' | 110.0 ms | 11.69 ms |  33.15 ms |  1.00 |    0.00 |
|                               'Query by DbEntity Exec Command' | 116.2 ms | 13.00 ms |  36.66 ms |  1.12 |    0.43 |
|                        'Query by DbEntity with Optimization 2' | 929.3 ms | 54.36 ms | 155.08 ms |  9.11 |    2.75 |
|       'Query by DbEntity without Children with Optimization 2' | 179.6 ms | 22.83 ms |  66.22 ms |  1.78 |    0.83 |
|     'Query by DbEntity with only Mkt Loss with Optimization 2' | 549.9 ms | 32.06 ms |  93.03 ms |  5.52 |    2.04 |
| 'Query by DbEntity with only User Session with Optimization 2' | 598.6 ms | 38.13 ms | 106.91 ms |  5.93 |    1.99 |

## 2020-01-09

By caching the delegates on walking all children and all parent, there was a little benefit on performances.

__With Caching__

|                                                         Method |      Mean |    Error |   StdDev | Ratio | RatioSD |
|--------------------------------------------------------------- |----------:|---------:|---------:|------:|--------:|
|                                'Baseline. Query by direct SQL' |  19.61 ms | 0.232 ms | 0.217 ms |  1.00 |    0.00 |
|                               'Query by DbEntity Exec Command' |  42.65 ms | 0.420 ms | 0.393 ms |  2.18 |    0.03 |
|                        'Query by DbEntity with Optimization 2' | 358.32 ms | 5.647 ms | 5.282 ms | 18.27 |    0.37 |
|       'Query by DbEntity without Children with Optimization 2' |  27.56 ms | 0.842 ms | 2.482 ms |  1.39 |    0.14 |
|     'Query by DbEntity with only Mkt Loss with Optimization 2' | 193.07 ms | 3.700 ms | 4.544 ms |  9.84 |    0.26 |
| 'Query by DbEntity with only User Session with Optimization 2' | 188.56 ms | 3.396 ms | 4.978 ms |  9.68 |    0.35 |

__Without Caching__

|                                                         Method |      Mean |    Error |   StdDev | Ratio | RatioSD |
|--------------------------------------------------------------- |----------:|---------:|---------:|------:|--------:|
|                                'Baseline. Query by direct SQL' |  17.44 ms | 0.270 ms | 0.252 ms |  1.00 |    0.00 |
|                               'Query by DbEntity Exec Command' |  40.49 ms | 0.494 ms | 0.462 ms |  2.32 |    0.04 |
|                        'Query by DbEntity with Optimization 2' | 350.14 ms | 5.717 ms | 5.348 ms | 20.08 |    0.47 |
|       'Query by DbEntity without Children with Optimization 2' |  27.75 ms | 0.757 ms | 2.233 ms |  1.57 |    0.13 |
|     'Query by DbEntity with only Mkt Loss with Optimization 2' | 193.62 ms | 3.787 ms | 5.896 ms | 11.16 |    0.39 |
| 'Query by DbEntity with only User Session with Optimization 2' | 193.59 ms | 3.814 ms | 5.220 ms | 11.05 |    0.39 |

## 2020-01-09

Diagnostic run

|                                                       Method |      Mean |    Error |   StdDev | Ratio | RatioSD |
|------------------------------------------------------------- |----------:|---------:|---------:|------:|--------:|
|                                Baseline. Query by direct SQL |  50.25 ms | 1.004 ms | 1.784 ms |  1.00 |    0.00 |
|                               Query by DbEntity Exec Command |  70.40 ms | 1.358 ms | 1.765 ms |  1.39 |    0.06 |
|                        Query by DbEntity with Optimization 2 | 580.18 ms | 4.450 ms | 4.163 ms | 11.30 |    0.40 |
|       Query by DbEntity without Children with Optimization 2 |  58.78 ms | 1.149 ms | 1.982 ms |  1.17 |    0.06 |
|     Query by DbEntity with only Mkt Loss with Optimization 2 | 335.06 ms | 2.713 ms | 2.538 ms |  6.53 |    0.26 |
| Query by DbEntity with only User Session with Optimization 2 | 333.85 ms | 4.728 ms | 3.691 ms |  6.43 |    0.21 |

## 2020-01-05

|                                   Method | Categories |         Mean |     Error |    StdDev |    Ratio | RatioSD |
|----------------------------------------- |----------- |-------------:|----------:|----------:|---------:|--------:|
|          'Baseline. Query by direct SQL' |       Full |     20.09 ms |  0.374 ms |  0.312 ms |     1.00 |    0.00 |
|         'Query by DbEntity Exec Command' |       Full |     42.46 ms |  0.844 ms |  0.972 ms |     2.14 |    0.06 |
| 'Query by DbEntity without Optimization' |       Full | 23,914.09 ms | 82.503 ms | 77.173 ms | 1,190.94 |   17.50 |
|  'Query by DbEntity with Optimization 1' |       Full |  6,822.02 ms | 52.633 ms | 49.233 ms |   339.55 |    5.56 |
|  'Query by DbEntity with Optimization 2' |       Full |    349.16 ms |  5.372 ms |  5.025 ms |    17.37 |    0.40 |
|                                          |            |              |           |           |          |         |
|          'Baseline. Query by direct SQL' |    Top 200 |     13.23 ms |  0.109 ms |  0.091 ms |     1.00 |    0.00 |
|         'Query by DbEntity Exec Command' |    Top 200 |     13.23 ms |  0.096 ms |  0.089 ms |     1.00 |    0.01 |
| 'Query by DbEntity without Optimization' |    Top 200 |  1,003.28 ms | 19.180 ms | 18.837 ms |    75.89 |    1.17 |
|  'Query by DbEntity with Optimization 1' |    Top 200 |    295.58 ms |  4.757 ms |  4.450 ms |    22.34 |    0.40 |
|  'Query by DbEntity with Optimization 2' |    Top 200 |     21.87 ms |  0.434 ms |  1.204 ms |     1.64 |    0.06 |

## 2020-01-03

|                                              Method |            Mean |         Error |        StdDev |          Median |  Ratio | RatioSD |
|---------------------------------------------------- |----------------:|--------------:|--------------:|----------------:|-------:|--------:|
|                     'Baseline. Query by direct SQL' |     16,697.6 us |     329.20 us |     274.90 us |     16,636.6 us |   1.00 |    0.00 |
|           'Query by direct SQL on CLR User Session' |        925.8 us |      18.23 us |      24.34 us |        929.6 us |   0.05 |    0.00 |
|                   'Query by direct SQL for Top 200' |     12,067.1 us |     156.23 us |     146.14 us |     12,047.3 us |   0.72 |    0.01 |
|           'Query by direct SQL in inner join query' |      1,270.3 us |      21.63 us |      19.18 us |      1,275.4 us |   0.08 |    0.00 |
|                         'Query by DbEntity with VM' |    221,491.1 us |   4,352.17 us |   4,071.02 us |    222,477.8 us |  13.23 |    0.32 |
|             'Query by DbEntity with VM for Top 200' |     18,532.6 us |     913.71 us |   2,650.83 us |     18,622.8 us |   1.18 |    0.18 |
| 'Query by DbEntity without VM without Optimization' | 10,413,662.5 us | 166,654.66 us | 155,888.87 us | 10,489,346.6 us | 623.06 |   17.21 |
|  'Query by DbEntity without VM with Optimization 1' |  2,323,780.1 us |  40,578.06 us |  37,956.74 us |  2,311,550.5 us | 138.65 |    3.42 |
|  'Query by DbEntity without VM with Optimization 2' |    229,413.8 us |   5,562.13 us |  14,357.64 us |    224,949.8 us |  13.67 |    0.98 |
|                'Query by DbEntity without Children' |     25,846.3 us |     961.95 us |   2,836.33 us |     26,165.2 us |   1.55 |    0.18 |
|              'Query by DbEntity with only Mkt Loss' |    117,922.0 us |   2,316.28 us |   3,537.20 us |    118,370.5 us |   7.13 |    0.25 |
|          'Query by DbEntity with only User Session' |    119,686.9 us |   2,325.58 us |   3,551.41 us |    120,035.7 us |   7.15 |    0.29 |
|             'Query by DbEntity Exec Cmd Dyn Result' |     38,492.1 us |     655.07 us |     612.75 us |     38,598.7 us |   2.30 |    0.05 |
|                 'Query by DbEntity with Inner Join' |    112,909.6 us |   4,305.26 us |  12,694.14 us |    111,878.0 us |   6.70 |    0.77 |
|             'Query by DbEntity On CLR User Session' |    179,963.7 us |   3,549.93 us |   7,641.59 us |    181,392.3 us |  10.75 |    0.46 |

## 2019-12-09

|                                     Method |         Mean |      Error |     StdDev |  Ratio | RatioSD |
|------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|
|            'Baseline. Query by direct SQL' |    19.722 ms |  0.3563 ms |  0.3333 ms |   1.00 |    0.00 |
|          'Query by direct SQL for Top 200' |    13.482 ms |  0.2148 ms |  0.2010 ms |   0.68 |    0.01 |
|  'Query by direct SQL in inner join query' |     1.449 ms |  0.0160 ms |  0.0149 ms |   0.07 |    0.00 |
|                'Query by DbEntity with VM' | 2,250.581 ms | 21.2673 ms | 19.8934 ms | 114.15 |    2.01 |
|    'Query by DbEntity with VM for Top 200' |   107.910 ms |  2.1429 ms |  3.0732 ms |   5.49 |    0.18 |
|             'Query by DbEntity without VM' | 2,254.462 ms | 26.6057 ms | 24.8870 ms | 114.35 |    2.41 |
|       'Query by DbEntity without Children' |    21.069 ms |  0.4418 ms |  1.0839 ms |   1.07 |    0.06 |
|     'Query by DbEntity with only Mkt Loss' |   617.693 ms | 12.0193 ms | 14.3082 ms |  31.40 |    0.98 |
| 'Query by DbEntity with only User Session' | 1,551.044 ms | 15.8362 ms | 14.8132 ms |  78.67 |    1.73 |
|    'Query by DbEntity Exec Cmd Dyn Result' |    42.434 ms |  0.5709 ms |  0.5340 ms |   2.15 |    0.04 |
|        'Query by DbEntity with Inner Join' |    20.191 ms |  1.4042 ms |  3.9374 ms |   1.19 |    0.13 |
