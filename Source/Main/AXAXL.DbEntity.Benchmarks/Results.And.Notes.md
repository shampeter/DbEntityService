# Benchmark Results and Notes

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
