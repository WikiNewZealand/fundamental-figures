# fundamental-figures

The console app requires two parameters:

1. The path to a .json configuraton file
2. A search term (enclose multi-word terms with double quotes)

e.g. `fundamental-figures config.json hamilton`

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
    "discriminator": "Name of the column that we search, defaults to 'Territorial Authority'",
    "value": "Name of the column that holds the value of each row, defaults to 'Value'",
    "valueUnit": "Not currently used, defaults to 'ValueUnit'",
    "valueLabel": "Not currently used, defaults to 'ValueLabel'",
    "measure": {
        "column": "Name of column that defines subsets of this dataset",
        "group": {
            "column": "Name of column used to further subdivide each subset",
            "separator": "Text to separate measure and group for display purposes, defaults to —",
            "include": [
                {
                "value": "Include row only if its group matches this value",
                "label": "[OPTIONAL] Transform row's group to this text for display purposes"
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
                "value": "Include row only if its measure matches this value"
            },
            {
                "value": "Include row only if its measure matches this other value",
                "label": "[OPTIONAL] Transform row's measure to this text for display purposes"
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
