# Orleans.TelemetryConsumers.ElasticSearch


| . | . |
| --- | --- |
| **PR Build** | [![Build status](https://ci.appveyor.com/api/projects/status/vtv4y6n8hmdbsrl5?svg=true)](https://ci.appveyor.com/project/OrleansContrib/orleans-telemetryconsumers-elasticsearch) |
| **Build** | [![Build status](https://ci.appveyor.com/api/projects/status/vtv4y6n8hmdbsrl5/branch/master?svg=true)](https://ci.appveyor.com/project/OrleansContrib/orleans-telemetryconsumers-elasticsearch/branch/master) |
| **NuGet** | [![nuget](https://img.shields.io/nuget/v/Orleans.TelemetryConsumers.ElasticSearch.svg)](https://www.nuget.org/packages/Orleans.TelemetryConsumers.ElasticSearch/) |
   
[![Build history](https://buildstats.info/appveyor/chart/OrleansContrib/resourcefitness)](https://ci.appveyor.com/project/OrleansContrib/orleans-telemetryconsumers-elasticsearch/history)


A telemetry consumer delivering data to ElasticSearch.  Each data point is written with a elastic type matching the Telemetry type (Log, Trace, Request, Metric).  The index for the data point is calculated with a \<prefix\>-yyyy-MM-dd-HH.  This makes it easy to delete older indexes (e.g. keeping only 3 days, or 3 hours, etc)

## Installation

```ps
Install-Package Orleans.TelemetryConsumers.ElasticSearch
```

## Usage

* get your elasticsearch url
* choose an index prefix


```cs
var elasticSearchURL = new Uri("http://elasticsearch:9200");

var esTeleM = new ElasticSearchTelemetryConsumer(elasticSearchURL, "orleans-telemetry");
LogManager.TelemetryConsumers.Add(esTeleM);
LogManager.LogConsumers.Add(esTeleM);

//then start your silo
siloHost = new SiloHost("primary", clusterConfig);
```

## Environmental setup

You need an ElasticSearch host, and likely you want Kibana to view the data

### setup your ElasticSearch host

try using docker

see https://elk-docker.readthedocs.io/

```bash
$ sudo docker run -p 5601:5601 -p 9200:9200 -p 5044:5044 -it --name elk sebp/elk
```
or a windows based E/K

docker-compose.yml

```
version: '2.1'

services:
  kibana:
    image: sixeyed/kibana:nanoserver
    ports: 
      - "5601:5601"
    depends_on:
      - elasticsearch
    hostname: kibana
  elasticsearch:
    image: sixeyed/elasticsearch:nanoserver
    ports:
      - "9200:9200"
      - "9300:9300"
    mem_limit: 8192m
    hostname: elasticsearch
networks:
  default:
    external:
      name: nat
```

and docker-compose up (thanks sixeyed)

### start your silo(s)

see https://gitter.im/dotnet/orleans or create an issue here for problems

### Configure ElasticSearch and Kibana

Note that `orleans-telemetry` was used for the index prefix

go the kibana managment page
http://kibana:5601/app/kibana#/management

click index patterns
http://kibana:5601/app/kibana#/management/kibana/indices

click +Add New
type in the prefix you used (see above) `orleans-telemetry` adding a `dash`

if you have data in ElasticSearch then it will display a Date field, which will be `UtcDateTime`


![](ES_metrics.png?raw=true)



