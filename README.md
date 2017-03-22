
setup your ElasticSearch host

usually you want create both a ElasticSeach and kibana to read the data

try using docker

see https://elk-docker.readthedocs.io/

$ sudo docker run -p 5601:5601 -p 9200:9200 -p 5044:5044 -it --name elk sebp/elk


            var elasticSearchURL = new Uri("http://192.168.1.1:9200");

            var esTeleM = new ElasticSearchTelemetryConsumer(elasticSearchURL, "orleans_telemetry");
            LogManager.TelemetryConsumers.Add(esTeleM);
            LogManager.LogConsumers.Add(esTeleM);
            
            
            then fire up your orleans silo
            
                        siloHost = new SiloHost("primary", clusterConfig);
