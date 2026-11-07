# Review origin/dev 33debce49b772dbfcf98fda3bd48a4f0954d2b9c

Reviewed all new source changes since `284a7145d7ce656b8935ca934ad85cd8dba0b8c9`, with Luna reviews of the local-player directory and reservation-card changes. Static source inspection only; no tests, builds, linters, application execution, or runtime validation.

## Text reservation posts render the same reservation as two full panels

Location: `src/BiliLite.UWP/Controls/Dynamic/DisplayModel.cs:186-187` and `src/BiliLite.UWP/Controls/Dynamic/DynamicItemControl.xaml:220`.

Trigger: open a user feed containing a `DYNAMIC_TYPE_WORD` post with `additional.type == ADDITIONAL_TYPE_RESERVE` and a populated `additional.reserve`. `Modules/User/DynamicVM.cs:2264` first projects that reserve into `Attachments`; its call at line 2293 then runs `DynamicParse.ParseOneRowInfo2024`, whose text branch changes the display type to `Reserve` and builds `OneRowInfo` from the same reserve. Nothing removes the already projected attachment.

Effect: the `Reserve` template in `DynamicTemplate.xaml:5-35` displays its reservation panel inside `CardContent`, then the shared `AttachmentRow` displays the same reservation again. The new unconditional reserve `ShowPanel` selection turns the existing extra summary row into a second full bordered panel with the same title, description and reservation button. Ordinary text reservation posts therefore repeat the entire card and take substantially more feed space. Image posts with a reservation attachment need the new panel, but text posts already promoted to `Reserve` need only one.

Suggested fix: give the reservation a single display owner. Once the final display type is `Reserve`, omit the matching reserve summary from `AttachmentItems`, preserving other attachments; alternatively let the shared attachment panel own the reservation and remove the duplicate standalone panel. Keep reservation attachment panels on photo and other card types. Apply the choice after text-to-reserve promotion so the earlier projection cannot retain a duplicate.

The previous local-playback picker finding is resolved by the directory wiring in `9c8920f`; it is not repeated. This range adds no evidence identifying BR-21's memory-growth cause or BR-53's failing interface response.
