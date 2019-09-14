# fundamental-figures

The console app requires two parameters:

1. The path to a .json configuraton file (enclose paths with spaces inside double quotes)
2. A search term (enclose multi-word terms inside double quotes)

e.g. `fundamental-figures fundamental-figures.json hamilton`

Other options:
- `-in|--input-path` to specify the local path where input .csv files will be cached, defaults to './input'
- `-out|--output-path` to specify the local path where the output file will be written, defaults to './output'
- `-t|--output-type` to select output file type, one or more of 'excel', 'json', 'csv', or 'yaml', default is 'excel'
- `-o|--open-output-file` to open the output file in your default file handler when processing is complete
- `-?|-h|--help` to show help information

Instead of running the console app once per search term, you can supply a newline-separated file of search terms as an @-file argument:

`fundamental-figures ..\fundamental-figures.json @..\territorial-authorities.txt -t excel -t json -t csv -t yaml`

The .json configuration file populates an instance of the `Figure` class. 

An empty `Figure` looks like this:

```
{
    "datasets": [
        
    ]
}
```

…where `datasets` is a collection of instances of the `Dataset` class. 

## Schema

We've provided the configuration file we use to build https://places.figure.nz/ in this reposistory (see [fundamental-figures.json](https://github.com/WikiNewZealand/fundamental-figures/blob/master/fundamental-figures.json)). You can use that file directly, or build your own configuration file.

A `Dataset` looks like this:

```
{
    "source": "https://figure.nz/table/xxxxxxxxxxxxxxxxxxxxx/download",
    "parent": "Name of section that this dataset will be displayed under in the output file",
    "selector": "Name of the column that we search, defaults to 'Territorial Authority'",
    "measure": [ // <-- List of columns that define the measure. More than one column indicates a "group by" operation
        {
            "name": "Name of column in the input .csv file",
            "separator": "Text to separate this measure column from subsequent measure columns in this measure for display purposes, defaults to ' — '",
            "include": [
                {
                    "value": "Include row only if the column matches this value",
                    "label": "Transform this column's value to this text for display purposes, defaults to 'null'",
                    "convert-to-percentage": false // <-- [OPTIONAL] Calculate each category's value as a percentage of the overall measurement total, defaults to 'false'
                }
            ],
            "exclude": [
                "Exclude row if the column matches this value",
                "Exclude row if the column matches this other value"
            ]
        }
    ],
    "category": [ // <-- List of columns that define the category. More than one column indicates a "group by" operation. Uses the same schema as "measure"
        {
            "name": "Name of column in the input .csv file",
            "include": [
                {
                    "value": "Include row only if the column matches this value"
                }
            ],
            "exclude": []
        }
    ],
    "value": "Name of the column that holds the value of each row, defaults to 'Value'",
    "valueUnit": "Name of the column that holds the value unit of each row, defaults to 'Value Unit'",
    "valueLabel": "Name of the column that holds the value label of each row, defaults to 'Value Label'",
    "nullReason": "Name of the column that holds the null reason for each row, defaults to 'Null Reason'",
    "date": "Name of column that contains the date of the measurement",
    "all-selectors-match-term": "If the input term equals this value assume *all* rows in this csv are candidates for processing, defaults to 'null' which means we'll compare the input term with the selector value for each row",
    "exclude-zero-values": false, // <-- Exclude zero values from the final grouped result set, defaults to 'false'
    "term-mapping": "Path to a file that defines alternate terms, e.g. map 'Auckland' to 'Auckland District Health Board' and 'Auckland City Council' to include rows with those terms as well. Defaults to null, which means no mappings will be loaded."
}
```

A starting point for a `dataset` is:

```
{
    "source": "https://figure.nz/table/xxxxxxxxxxxxxxxxxxxxx/download",
    "parent": "",
    "selector": "Territorial Authority",
    "measure": [
        {
            "name": "",
            "separator": " — ",
            "include": [
                {
                    "value": "",
                    "label": ""
                },
                {
                    "value": "",
                    "label": ""
                }
            ],
            "exclude": [
                "",
                ""
            ]
        },
        {
            "name": "",
            "include": [
                {
                    "value": "",
                    "label": ""
                },
                {
                    "value": "",
                    "label": ""
                }
            ],
            "exclude": [
                "",
                ""
            ]
        }        
    ],
    "category": [
        {
            "name": "",
            "include": [
                {
                    "value": "",
                    "label": ""
                },
                {
                    "value": "",
                    "label": ""
                }
            ],
            "exclude": [
                "",
                ""
            ]
        }        
    ],
    "date": ""
}
```

## Processing files for a Territorial Authority that contain only 'wards' 

To solve the "Auckland problem", where Auckland has its own csv file for a `dataset` that contains individual Local Board Areas, create a duplicate `dataset` and set the `all-selectors-match-term` value:

```
{
    "uri": "https://figure.nz/table/xxxxxxxxxxxxxxxxxxxxy/download",
    ...
    "all-selectors-match-term": "Auckland",
    "selector": "Local Board Area", // <-- The column that holds the selectors we want to match
    ...
}

```

## Term Mapping files

We've provided two examples of mapping files in this repository:

- [territorial-authorities-mapping.json](https://github.com/WikiNewZealand/fundamental-figures/blob/master/territorial-authorities-mapping.json)
- [territorial-authorities-dhb-mapping.json](https://github.com/WikiNewZealand/fundamental-figures/blob/master/territorial-authorities-dhb-mapping.json)

A term mapping file looks like this:

```
{
  "Auckland": [
    "Auckland District Health Board",
    "Auckland City Council"
  ],
  "Wellington": [
    "Capital and Coast District Health Board",
    "Wellington City Council"
  ]
}
```

## Migrating an old .json configuration file to new schema

Use the `FigureNZ.FundamentalFigures.Migrator` project. It's a console app that takes the path to your old configuration file, and writes a `-migrated.json` version of the file to the same location.

e.g. `fundamental-figures-migrator ../fundamental-figures.json`