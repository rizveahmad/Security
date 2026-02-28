# Security — JSON Form Builder

A .NET 10 MVC application implementing a reusable JSON-based form builder that injects dynamic extension fields into core master forms (Customers, Products, Contacts, …).

---

## Architecture

```
Security.Domain          ← domain entities (FormDefinition, FormFieldDefinition, FormSubmission, ApplicationUser)
Security.Application     ← interfaces (IFormBuilderService, IAppDbContext, ValidationResult)
Security.Infrastructure  ← EF Core + Identity (AppDbContext, FormBuilderService, DbInitializer)
Security.Web             ← MVC controllers + Razor views
```

---

## Getting Started

```bash
# Build
dotnet build Security.slnx

# Run (creates security.db + seeds SuperAdmin)
cd src/Security.Web
dotnet run
```

Set the SuperAdmin password via user-secrets or environment variable:

```bash
dotnet user-secrets set "Seed:SuperAdminPassword" "YourPassword@1"
```

Default seed: `superadmin@security.local` / `Admin@12345!`

---

## Phase 1 — Form Definition Model

| Entity | Purpose |
|---|---|
| `FormDefinition` | One record per menu key (e.g. `customers`). Stores versioned `FieldsJson`. |
| `FormFieldDefinition` | POCO deserialized from `FieldsJson`. Fields: key, label, type, required, placeholder, helpText, options, order. |
| `FormSubmission` | Stores per-record extension field values as `ValuesJson` keyed by `MenuKey + RecordKey`. |

**Supported field types:** `Text`, `Number`, `Dropdown`, `Date`, `Checkbox`, `Textarea`

---

## Phase 2 — Designer & Renderer

### Designer (`/FormBuilder/Designer?menuKey=<key>`)

- Protected by `[Authorize(Roles="SuperAdmin,Admin")]`
- **Left panel**: Field type palette (click to add)
- **Centre canvas**: SortableJS drag-to-reorder list (SRI-pinned CDN)
- **Right panel**: Property editor — label, key, required, placeholder, help text, dropdown options
- **Save**: Serialises current field array as JSON → POST `/FormBuilder/Save`

### Reusable Renderer — `Views/Shared/_FormFields.cshtml`

```cshtml
@* Include inside any master form's <fieldset> *@
@{
    ViewData["ExtValues"] = extensionValuesDictionary; // Dictionary<string, string?>
}
@await Html.PartialAsync("_FormFields", extensionFieldList)
```

Renders all 6 field types, shows required markers, renders validation errors from `ModelState`.

---

## Phase 3 — Integration into Core Menu Forms

### Where the form builder hooks in

| Location | File | Description |
|---|---|---|
| Fixed fields | `Views/Customers/Create.cshtml` lines 13–28 | `<fieldset>` with hardcoded Name and Email inputs |
| Extension region | `Views/Customers/Create.cshtml` lines 30–41 | Calls `_FormFields` partial with `ViewBag.ExtensionFields` |
| Load extension fields | `CustomersController.Create (GET)` | `var fields = await _formBuilder.GetFieldsAsync("customers");` |
| Validate extension fields | `CustomersController.Create (POST)` | `var validation = await _formBuilder.ValidateAsync("customers", extValues);` |
| Save extension values | `CustomersController.Create (POST)` | `await _formBuilder.SaveSubmissionAsync("customers", recordKey, extValues);` |
| Form prefix convention | All extension `<input name="…">` | Prefixed `ext_` so controller can strip and separate from fixed fields |

### Adding form builder to another menu

1. Inject `IFormBuilderService` into the controller.
2. `GET` action: call `GetFieldsAsync(menuKey)` → put in `ViewBag.ExtensionFields`.
3. View: wrap `@await Html.PartialAsync("_FormFields", extFields)` inside a `<fieldset>`.
4. `POST` action: call `ExtractExtensionValues(form)` (strip `ext_` prefix), then `ValidateAsync` + `SaveSubmissionAsync`.
5. Navigate to `/FormBuilder/Designer?menuKey=<key>` to design the extension fields.

### Quick-insert modal

`Views/Shared/_QuickInsert.cshtml` provides a reusable Bootstrap modal + JS helpers:

- `openQuickInsert(sourceUrl, targetFieldId)` — loads a partial form via AJAX
- `quickInsertDone(value, text, targetFieldId)` — appends option to target `<select>` and closes modal
- Access to quick insert should be gated by checking the user's role against the source menu's permission before rendering the trigger button.

---

## Remaining Gaps (explicit)

- Only the **Customers** menu is wired up; Products/Contacts follow the same pattern (documented above).
- Dynamic dropdown data sources (`DynamicSource` field) are modelled but not yet rendered — static options only.
- Quick-insert submit endpoint (save + return `value`/`text`) needs to be implemented per source menu.
- Full CRUD list/edit pages for Customers are stubs; real persistence depends on the domain tables you add.
- File upload field type not yet included.
