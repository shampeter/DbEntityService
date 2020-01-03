# Benchmark Results and Notes

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
