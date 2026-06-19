Config = {}

-- Command that opens the menu (/sirentest by default).
Config.Command = 'sirentest'

-- Optional keybind. Leave as '' to disable. Players can rebind it under
-- Settings > Key Bindings > FiveM once set (e.g. 'F7').
Config.Keybind = ''

-- Soundsets in your generated SirenSharp pack.
--
-- FiveM has no native to enumerate which soundsets a dat54 registered, so this
-- list drives the menu and MUST match the names in your pack. SirenSharp can
-- generate this file for you (Generate Resource > "Generate in-game tester").
--
--   dlc    = your DLC name WITHOUT the dlc_ prefix (the export "DLC Name")
--   name   = the AWC / soundset name
--   sirens = the siren (ScriptName) entries inside that soundset
--
-- In-game these resolve to:
--   soundset native name : '<name>_soundset'
--   LVC Bank path        : 'dlc_<dlc>/<name>'
Config.Soundsets = {
    {
        dlc = 'policesirens',
        name = 'lspd',
        sirens = { 'wail', 'yelp', 'airhorn' },
    },
    {
        dlc = 'policesirens',
        name = 'bcso',
        sirens = { 'wail', 'yelp', 'phaser' },
    },
}
