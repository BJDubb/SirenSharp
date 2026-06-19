# Vendored RageUI

This is a pinned, unmodified copy of RageUI bundled with the SirenSharp in-game
tester. RageUI is archived/unmaintained upstream, so we ship our own copy rather
than depend on it being available.

- Source: https://github.com/ImBaphomettt/RageUI
- Pinned commit: 1fda5eb28303509c03103a20395e430a0caf1711
- Authors: Dylan MALANDAIN, Kalyptus

Only `src/` is vendored; RageUI's own `fxmanifest.lua` and `example.lua` are
not used - the tester's top-level `fxmanifest.lua` loads these files directly.
