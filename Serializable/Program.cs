using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MessagePack;
using ProtoBuf;
using SharpYaml.Serialization.Serializers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Serializable
{
    [XmlRoot("dictionary")]
    
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var keySerializer = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty) return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                var key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                var value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            var keySerializer = new XmlSerializer(typeof(TKey));
            var valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }

    [MessagePackObject]
    [ProtoContract]
    [DataContract]
    [Serializable]
    public class Player
    {
        [Key(5)]
        [ProtoMember(1)]
        [DataMember]
        public string namePlayer;
        [Key(6)]
        [ProtoMember(2)]
        [DataMember]
        public double salary;

        public Player()
        { }
        public Player(string nameplayer, double sal)
        {
            namePlayer = nameplayer;
            salary = sal;
        }
    }

    [MessagePackObject]
    [ProtoContract]
    [DataContract]
    [Serializable]
    public class FootballClub
    {
        [Key(0)]
        [ProtoMember(1)]
        [DataMember]
        public string nameClub;

        [Key(1)]
        [ProtoMember(2)]
        [DataMember]
        public int yearOfFoundation;

        [Key(2)]
        [ProtoMember(3)]
        [DataMember]
        public double capitalization;

        [Key(3)]
        [ProtoMember(4)]
        [DataMember]
        public Player[] players;

        [Key(4)]
        [ProtoMember(5)]
        [DataMember]
        [XmlArray]
        [SharpYaml.Serialization.YamlMember]
        public SerializableDictionary<int, string> likeDate = new SerializableDictionary<int, string>();

        FootballClub()
        { }

        public FootballClub(string nameclub, int yearoffoundation, double capitaliz, Player[] players_club)
        {
            nameClub = nameclub;
            yearOfFoundation = yearoffoundation;
            capitalization = capitaliz;
            players = players_club;
            likeDate.Add(1, "День рождения клуба");
            likeDate.Add(2, "Новый год");
        }
    }

    class Program
    {
        public void print()
        {
            Console.WriteLine(
                "Добрый день. Выберите действие:\n" +
                "1) Протестировать нативную.\n" +
                "2) Протестировать XML.\n" +
                "3) Протестировать JSON.\n" +
                "4) Протестировать Google Protocol Buffers.\n" +
                "5) Протестировать MessagePack.\n" +
                "6) Протестировать Yaml.\n" +
                "0) Выйти.");
        }

        public void native(FootballClub club)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream memorystream = new MemoryStream();
            Stopwatch swatch = new Stopwatch();

            Console.WriteLine("----Вычисляем размер серилизованного объекта----");
            formatter.Serialize(memorystream, club);
            long size = memorystream.Length;

            Console.WriteLine("Вычисляем среднее время серилизации----");
            swatch.Reset();
            for (int i = 0; i < 20000; i++)
            {
                memorystream.Position = 0;
                swatch.Start();
                formatter.Serialize(memorystream, club);
                swatch.Stop();
            }
            TimeSpan time = swatch.Elapsed;
            double timeSer = time.TotalMilliseconds / 20000;

            Console.WriteLine("----Серелизуем объект в файл----");
            using (FileStream fs = new FileStream("club.dat", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, club);
            }

            Console.WriteLine("Вычисляем среднее время десерилизации----");
            swatch.Reset();
            FootballClub clubDis = null;
            using (FileStream fs = new FileStream("club.dat", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < 20000; i++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Position = 0;
                    swatch.Start();
                    clubDis = (FootballClub)formatter.Deserialize(fs);
                    swatch.Stop();
                }

            }
            time = swatch.Elapsed;
            double timeDis = time.TotalMilliseconds / 20000;

            Console.WriteLine("Десилиризованный объект имя: " + clubDis.nameClub);

            Console.WriteLine("Итог: размер серилизованного объекта = " + size + "\n" +
                "Среднее время серелизации = " + timeSer + "\n" +
                "Среднее время десерелизации = " + timeDis);
        }

        public void xml(FootballClub club)
        {
            DataContractSerializer formatter = new DataContractSerializer(typeof(FootballClub));
            MemoryStream memorystream = new MemoryStream();
            Stopwatch swatch = new Stopwatch();

            Console.WriteLine("----Вычисляем размер серилизованного объекта----");
            formatter.WriteObject(memorystream, club);
            long size = memorystream.Length;

            Console.WriteLine("Вычисляем среднее время серилизации----");
            swatch.Reset();
            for (int i = 0; i < 20000; i++)
            {
                memorystream.Position = 0;
                swatch.Start();
                formatter.WriteObject(memorystream, club);
                swatch.Stop();
            }
            TimeSpan time = swatch.Elapsed;
            double timeSer = time.TotalMilliseconds / 20000;

            Console.WriteLine("----Серелизуем объект в файл----");
            using (FileStream fs = new FileStream("club.xml", FileMode.OpenOrCreate))
            {
                formatter.WriteObject(fs, club);
            }
            Console.WriteLine("Вычисляем среднее время десерилизации----");
            swatch.Reset();
            FootballClub clubDis = null;
            using (FileStream fs = new FileStream("club.xml", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < 20000; i++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Position = 0;
                    swatch.Start();
                    clubDis = (FootballClub)formatter.ReadObject(fs);
                    swatch.Stop();
                }

            }
            time = swatch.Elapsed;
            double timeDis = time.TotalMilliseconds / 20000;
            
            Console.WriteLine("Десилиризованный объект имя: " + clubDis.nameClub);

            Console.WriteLine("Итог: размер серилизованного объекта = " + size + "\n" +
                "Среднее время серелизации = " + timeSer + "\n" +
                "Среднее время десерелизации = " + timeDis);
        }

        public void json(FootballClub club)
        {
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(FootballClub));
            MemoryStream memorystream = new MemoryStream();
            Stopwatch swatch = new Stopwatch();

            Console.WriteLine("----Вычисляем размер серилизованного объекта----");
            jsonFormatter.WriteObject(memorystream, club);
            long size = memorystream.Length;

            Console.WriteLine("Вычисляем среднее время серилизации----");
            swatch.Reset();
            for (int i = 0; i < 20000; i++)
            {
                memorystream.Position = 0;
                swatch.Start();
                jsonFormatter.WriteObject(memorystream, club);
                swatch.Stop();
            }
            TimeSpan time = swatch.Elapsed;
            double timeSer = time.TotalMilliseconds / 20000;

            Console.WriteLine("----Серелизуем объект в файл----");
            using (FileStream fs = new FileStream("club.json", FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(fs, club);
            }

            Console.WriteLine("Вычисляем среднее время десерилизации----");
            swatch.Reset();
            FootballClub clubDis = null;
            using (FileStream fs = new FileStream("club.json", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < 20000; i++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Position = 0;
                    swatch.Start();
                    clubDis = (FootballClub)jsonFormatter.ReadObject(fs);
                    swatch.Stop();
                }

            }
            time = swatch.Elapsed;
            double timeDis = time.TotalMilliseconds / 20000;

            Console.WriteLine("Десилиризованный объект имя: " + clubDis.nameClub);

            Console.WriteLine("Итог: размер серилизованного объекта = " + size + "\n" +
                "Среднее время серелизации = " + timeSer + "\n" +
                "Среднее время десерелизации = " + timeDis);
        }

        public void protobuf(FootballClub club)
        {
            MemoryStream memorystream = new MemoryStream();
            Stopwatch swatch = new Stopwatch();

            Console.WriteLine("----Вычисляем размер серилизованного объекта----");
            ProtoBuf.Serializer.Serialize<FootballClub>(memorystream, club);
            long size = memorystream.Length;

            Console.WriteLine("Вычисляем среднее время серилизации----");
            swatch.Reset();
            for (int i = 0; i < 20000; i++)
            {
                memorystream.Position = 0;
                swatch.Start();
                ProtoBuf.Serializer.Serialize<FootballClub>(memorystream, club);
                swatch.Stop();
            }
            TimeSpan time = swatch.Elapsed;
            double timeSer = time.TotalMilliseconds / 20000;

            Console.WriteLine("----Серелизуем объект в файл----");
            using (FileStream fs = new FileStream("club.bins", FileMode.OpenOrCreate))
            {
                ProtoBuf.Serializer.Serialize(fs, club);
            }

            Console.WriteLine("Вычисляем среднее время десерилизации----");
            swatch.Reset();
            FootballClub clubDis = null;
            using (FileStream fs = new FileStream("club.bins", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < 20000; i++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Position = 0;
                    swatch.Start();
                    clubDis = ProtoBuf.Serializer.Deserialize<FootballClub>(fs);
                    swatch.Stop();
                }

            }
            time = swatch.Elapsed;
            double timeDis = time.TotalMilliseconds / 20000;

            Console.WriteLine("Десилиризованный объект имя: " + clubDis.nameClub);

            Console.WriteLine("Итог: размер серилизованного объекта = " + size + "\n" +
                "Среднее время серелизации = " + timeSer + "\n" +
                "Среднее время десерелизации = " + timeDis);
        }

        public void messagePack(FootballClub club)
        {
            var formatter = MessagePackSerializer.Serialize(club);
            MemoryStream memorystream = new MemoryStream();
            Stopwatch swatch = new Stopwatch();

            Console.WriteLine("----Вычисляем размер серилизованного объекта----");
            memorystream.Write(formatter, 0, formatter.Length);
            long size = memorystream.Length;

            Console.WriteLine("Вычисляем среднее время серилизации----");
            swatch.Reset();
            for (int i = 0; i < 20000; i++)
            {
                memorystream.Position = 0;
                swatch.Start();
                MessagePackSerializer.Serialize(club);
                swatch.Stop();
            }
            TimeSpan time = swatch.Elapsed;
            double timeSer = time.TotalMilliseconds / 20000;

            Console.WriteLine("----Серелизуем объект в файл----");
            using (FileStream fs = new FileStream("club.msgp", FileMode.OpenOrCreate))
            {
                fs.Write(formatter, 0, formatter.Length);
            }

            Console.WriteLine("Вычисляем среднее время десерилизации----");
            swatch.Reset();
            FootballClub clubDis = null;
            using (FileStream fs = new FileStream("club.msgp", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < 20000; i++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Position = 0;
                    swatch.Start();
                    clubDis = MessagePackSerializer.Deserialize<FootballClub>(fs);
                    swatch.Stop();
                }

            }
            time = swatch.Elapsed;
            double timeDis = time.TotalMilliseconds / 20000;

            Console.WriteLine("Десилиризованный объект имя: " + clubDis.nameClub);

            Console.WriteLine("Итог: размер серилизованного объекта = " + size + "\n" +
                "Среднее время серелизации = " + timeSer + "\n" +
                "Среднее время десерелизации = " + timeDis);
        }

        public void yaml(FootballClub club)
        {
            Stopwatch swatch = new Stopwatch();

            var serializer = new SharpYaml.Serialization.Serializer();

            Console.WriteLine("----Вычисляем размер серилизованного объекта----");
            var text = serializer.Serialize(club);


            var deserializer = new DeserializerBuilder()
             .WithNamingConvention(new CamelCaseNamingConvention())
             .Build();

            //FootballClub clubs = deserializer.Deserialize<FootballClub>(File.OpenText("club.yaml"));
            YamlDotNet.Serialization.Serializer serializers = new YamlDotNet.Serialization.Serializer();
            var tt = serializer.Serialize(club);
            long size = text.Length;

            Console.WriteLine("Вычисляем среднее время серилизации----");
            swatch.Reset();
            for (int i = 0; i < 20000; i++)
            {
                swatch.Start();
                serializer.Serialize(club);
                swatch.Stop();
            }
            TimeSpan time = swatch.Elapsed;
            double timeSer = time.TotalMilliseconds / 20000;

            byte[] bytes = Encoding.Unicode.GetBytes(text);

            Console.WriteLine("----Серелизуем объект в файл----");
            using (FileStream fs = new FileStream("club.yaml", FileMode.OpenOrCreate))
            {
                fs.Write(bytes, 0, bytes.Length);
            }

            Console.WriteLine("Вычисляем среднее время десерилизации----");
            swatch.Reset();
            FootballClub clubDis = null;
            using (FileStream fs = new FileStream("club.yaml", FileMode.OpenOrCreate))
            {
                for (int i = 0; i < 20000; i++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Position = 0;
                    swatch.Start();
                    clubDis = serializer.Deserialize<FootballClub>(fs);
                    
                    swatch.Stop();
                }

            }
            time = swatch.Elapsed;
            double timeDis = time.TotalMilliseconds / 20000;

            Console.WriteLine("Десилиризованный объект имя: " + clubDis.nameClub);

            Console.WriteLine("Итог: размер серилизованного объекта = " + size + "\n" +
                "Среднее время серелизации = " + timeSer + "\n" +
                "Среднее время десерелизации = " + timeDis);
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            int answer = 0;
            Player[] players = new Player[] {
                new Player("Иван", 300.2),
                new Player("Вова", 150.5)
            };
            FootballClub club = new FootballClub("Спартак", 1922, 100000.5, players);

            p.print();
            while (true)
            {
                try
                {
                    answer = Int32.Parse(Console.ReadLine());
                }
                catch
                {
                    Console.WriteLine("Вы ввели не число!");
                    continue;
                }

                switch (answer)
                {
                    case 1:
                        p.native(club);
                        break;
                    case 2:
                        p.xml(club);
                        break;
                    case 3:
                        p.json(club);
                        break;
                    case 4:
                        p.protobuf(club);
                        break;
                    case 5:
                        p.messagePack(club);
                        break;
                    case 6:
                        p.yaml(club);
                        break;
                    case 0:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Вы ввели число не из списка!");
                        break;
                }

                Console.WriteLine("Нажмите любую клавишу, чтобы попробовать что-то ещё!");
                Console.ReadKey();
                Console.Clear();
                p.print();
            }
        }
    }
}
