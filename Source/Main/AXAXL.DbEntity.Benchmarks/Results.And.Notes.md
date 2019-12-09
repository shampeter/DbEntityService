# Benchmark Results and Notes

## 2019-12-09

|                                     Method |         Mean |      Error |     StdDev |  Ratio | RatioSD |
|------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|
|            'Baseline. Query by direct SQL' |    19.667 ms |  0.1837 ms |  0.1719 ms |   1.00 |    0.00 |
|  'Query by direct SQL in inner join query' |     1.455 ms |  0.0090 ms |  0.0080 ms |   0.07 |    0.00 |
|                'Query by DbEntity with VM' | 2,265.760 ms | 23.7564 ms | 22.2217 ms | 115.21 |    1.18 |
|             'Query by DbEntity without VM' | 2,266.402 ms | 18.4623 ms | 17.2697 ms | 115.24 |    1.19 |
|       'Query by DbEntity without Children' |    25.723 ms |  0.7078 ms |  2.0758 ms |   1.31 |    0.12 |
|     'Query by DbEntity with only Mkt Loss' |   613.747 ms | 12.2538 ms | 13.6200 ms |  31.22 |    0.84 |
| 'Query by DbEntity with only User Session' | 1,576.150 ms | 14.2781 ms | 13.3558 ms |  80.15 |    1.07 |
|    'Query by DbEntity Exec Cmd Dyn Result' |    42.290 ms |  0.3534 ms |  0.3306 ms |   2.15 |    0.03 |
|        'Query by DbEntity with Inner Join' |    20.166 ms |  1.4372 ms |  4.0301 ms |   1.23 |    0.15 |
