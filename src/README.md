bombardier.exe  -c 5 -d 5s -l http://localhost:6000/slow
Bombarding http://localhost:6000/slow for 5s using 5 connection(s)
[==================================================================================================================] 5s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec         5.34      25.05     189.63
  Latency         1.03s      1.90s      6.40s
  Latency Distribution
     50%    16.90ms
     75%    21.67ms
     90%      4.85s
     95%      4.85s
     99%      6.40s
  HTTP codes:
    1xx - 0, 2xx - 31, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:     1.34KB/s

bombardier.exe  -c 5 -d 5s -l http://localhost:6000/fast
Bombarding http://localhost:6000/fast for 5s using 5 connection(s)
[==================================================================================================================] 5s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec     54797.43    7523.22   63799.36
  Latency       90.17us    84.93us    19.00ms
  Latency Distribution
     50%     0.00us
     75%     0.00us
     90%   504.00us
     95%     1.00ms
     99%     1.00ms
  HTTP codes:
    1xx - 0, 2xx - 274223, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:    13.34MB/s


bombardier -c 5 -d 5s -H 'Content-Type: application/json' -data '{"name": "string"}' -m POST http://localhost:6000/ 