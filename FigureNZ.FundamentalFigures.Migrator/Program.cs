using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FigureNZ.FundamentalFigures.Migrator
{
    class Program
    {
        static void Main(string[] args)
        {
            FileInfo source = new FileInfo(args[0]);

            Figure destination = new Figure
            {
                Datasets = new List<Dataset>()
            };

            foreach (Dataset1 dataset1 in JsonConvert.DeserializeObject<Figure1>(File.ReadAllText(source.FullName)).Datasets)
            {
                Dataset dataset = new Dataset
                {
                    Source = dataset1.Uri,
                    Parent = dataset1.Parent,
                    Selector = dataset1.Discriminator,
                    Measure = new List<Column>(),
                    Category = new List<Column>(),
                    Value = dataset1.Value,
                    ValueUnit = dataset1.ValueUnit,
                    ValueLabel = dataset1.ValueLabel,
                    NullReason = dataset1.NullReason,
                    Date = dataset1.Date,
                    AllSelectorsMatchTerm = dataset1.Term,
                    TermMapping = dataset1.TermMapping,
                    ExcludeZeroValues = dataset1.ExcludeZeroValues
                };

                dataset.Measure.Add(new Column
                {
                    Name = dataset1.Measure.Column,
                    Include = dataset1.Measure.Include,
                    Exclude = dataset1.Measure.Exclude
                });

                if (dataset1.Measure.Group != null)
                {
                    dataset.Measure[0].Separator = " " + dataset1.Measure.Group.Separator + " ";

                    dataset.Measure.Add(new Column
                    {
                        Name = dataset1.Measure.Group.Column,
                        Include = dataset1.Measure.Group.Include,
                        Exclude = dataset1.Measure.Group.Exclude
                    });
                }

                if (dataset1.Category != null)
                {
                    dataset.Category.Add(new Column
                    {
                        Name = dataset1.Category.Column,
                        Include = dataset1.Category.Include,
                        Exclude = dataset1.Category.Exclude
                    });
                }

                destination.Datasets.Add(dataset);
            }

            using (FileStream file = new FileStream(Path.Combine(source.DirectoryName, Path.GetFileNameWithoutExtension(source.Name) + "-migrated.json"), FileMode.Create))
            using (StreamWriter writer = new StreamWriter(file, Encoding.UTF8))
            using (JsonWriter json = new JsonTextWriter(writer))
            {
                new JsonSerializer { Formatting = Formatting.Indented,  NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() }.Serialize(json, destination);
            }
        }
    }
}
