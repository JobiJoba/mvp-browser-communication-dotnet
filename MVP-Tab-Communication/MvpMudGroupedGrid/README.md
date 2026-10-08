# MudBlazor grouped editable grid MVP

Recreates a Dutch “duur” spreadsheet-style grid with **two-level column headers** and **inline editing**, using MudBlazor `MudTable`.

## Run

```bash
dotnet run --project MvpMudGroupedGrid
```

Open http://localhost:5215

## What’s demonstrated

| Feature | Approach |
| --- | --- |
| Group headers (`Niet verminderde duur` / `Verminderde duur` spanning Jaren–Weken) | `MudTable` + `CustomHeader` + `MudTHeadRow` with `colspan` / `rowspan` |
| Inline edit | `MudTextField` / `MudNumericField` / `MudDatePicker` in each cell |
| Calculated columns | `Teller niet/verminderd` are read-only sums of the duration parts |
| Thick group borders | CSS on `.duur-group-niet` / `.duur-group-wel` |

MudBlazor `MudDataGrid` does not support multi-row *column* group headers (its “grouping” is row-based), so this MVP uses `MudTable` for the header layout.
