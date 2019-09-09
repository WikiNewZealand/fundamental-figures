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

A `Dataset` looks like this:

```
{
    "uri": "https://figure.nz/table/xxxxxxxxxxxxxxxxxxxxx/download",
    "parent": "Name of section that this dataset will be displayed under in the output file",
    "term": "If the input term equals this value assume *all* rows in this csv are candidates for processing, defaults to 'null' which means we'll compare the input term with the discriminator value for each row",
    "term-mapping": "Path to a file that defines alternate terms, e.g. map 'Auckland' to 'Auckland District Health Board' and 'Auckland City Council' to include rows with those terms as well. Defaults to null, which means no mappings will be loaded.",
    "discriminator": "Name of the column that we search, defaults to 'Territorial Authority'",
    "value": "Name of the column that holds the value of each row, defaults to 'Value'",
    "valueUnit": "Name of the column that holds the value unit of each row, defaults to 'ValueUnit'",
    "valueLabel": "Name of the column that holds the value label of each row, defaults to 'ValueLabel'",
    "exclude-zero-values": false, // <-- Exclude zero values from the final grouped result set, defaults to false
    "measure": {
        "column": "Name of column that defines subsets of this dataset",
        "group": {
            "column": "Name of column used to further subdivide each subset",
            "separator": "Text to separate measure and group for display purposes, defaults to —",
            "include": [
                {
                    "value": "Include row only if its group matches this value",
                    "label": "[OPTIONAL] Transform row's group to this text for display purposes",					
                },
                {
                    "value": "Include row only if its group matches this other row"
                }
            ],
            "exclude": [
                "Exclude row if group matches this value",
                "Exclude row if group matches this other value"
            ]
        },
        "include": [
            {
		"value": "Include row only if its measure matches this value",
		"label": "[OPTIONAL] Transform row's measure to this text for display purposes"
		"convert-to-percentage": true // <-- [OPTIONAL] Calculate each category's value as a percentage of the overall measurement + group total
            },
            {
                "value": "Include row only if its measure matches this other value"
            }
        ],
        "exclude": [
            "Exclude row if measure matches this value",
            "Exclude row if measure matches this other value"
        ]
    },
    "category": {
        "column": "Name of column that defines the value we're measuring within the subset",
        "include": [
            {
                "value": "Include row only if its category matches this value"
            },
            {
                "value": "Include row only if its category matches this other value",
                "label": "[OPTIONAL] Transform row's category to this text for display purposes"
            }
        ],
        "exclude": [
            "Exclude row if category matches this value",
            "Exclude row if category matches this other value"
        ]
    },
    "date": "Name of column that contains the date of the measurement"
}
```

A starting point for a `dataset` is:

```
{
    "uri": "https://figure.nz/table/xxxxxxxxxxxxxxxxxxxxx/download",
    "parent": "",
    "discriminator": "Territorial Authority",
    "measure": {
        "column": "",
        "group": {
            "column": "",
            "separator": "—",
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
    "category": {
        "column": "",
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
    "date": ""
}
```

To solve the "Auckland problem", where Auckland has its own csv file for a `dataset` that contains individual Local Board Areas, create a duplicate `dataset` and use the `term` and `discriminator` values:

```
{
    "uri": "https://figure.nz/table/xxxxxxxxxxxxxxxxxxxxy/download",
    ...
    "term": "Auckland",
    "discriminator": "Local Board Area",
    ...
}

```

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
