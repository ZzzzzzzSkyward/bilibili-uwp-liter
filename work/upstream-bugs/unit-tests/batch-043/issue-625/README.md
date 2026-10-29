# Issue #625 isolated source probe

Run `run-ugc-collection-comment-dispatch.sh` from this checkout. The probe executes the extracted mapper assignment from `DynUgcSeason.Avid` to `ugc_season.aid`, the display row's AID assignment, and the complete target open-comment handler with a recording `Utils.ShowComment` boundary.

Its fixture gives the dynamic and video AID distinct numeric values. It verifies that a `合集` card dispatches Video comments with the mapped video AID while the display model retains the dynamic ID in `ReplyID`. The probe does not execute the complete mapper/converter, UWP app, or comment API.
