# Implementation notes

## Why `MudTable` instead of `MudDataGrid`

MudBlazor’s `MudDataGrid` “grouping” is **row** grouping (collapse items by a property). It does **not** support Excel-style **multi-row column headers** with `colspan` / `rowspan`.

This MVP uses:

```razor
<MudTable CustomHeader="true" Breakpoint="Breakpoint.None" ...>
  <HeaderContent>
    <MudTHeadRow>
      <MudTh rowspan="2">...</MudTh>
      <MudTh colspan="4">Niet verminderde duur</MudTh>
      ...
    </MudTHeadRow>
    <MudTHeadRow>
      <MudTh>Jaren</MudTh> ...
    </MudTHeadRow>
  </HeaderContent>
  <RowTemplate>
    <!-- MudTextField / MudNumericField / MudDatePicker per cell -->
  </RowTemplate>
</MudTable>
```

`Breakpoint.None` disables the responsive stacked layout so the two-level header stays visible on all widths.

## Inline editing

Cells are always editable (spreadsheet-style), not click-to-edit-row:

| Column | Control |
| --- | --- |
| Maximale duur, Tantième, Bron | `MudTextField` |
| Jaren / Kwartalen / Maanden / Weken | `MudNumericField` (`nl-BE` culture) |
| Datum | `MudDatePicker` (`dd/MM/yyyy`) |
| Teller niet/verminderd | Read-only sum of that duration group |

## Styling

`wwwroot/app.css` styles `.duur-th` (grey header, centered) and thicker inset borders on `.duur-group-niet` / `.duur-group-wel` to approximate the boxed duration groups from the reference UI.
