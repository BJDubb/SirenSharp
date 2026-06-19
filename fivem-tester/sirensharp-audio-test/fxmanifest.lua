fx_version 'cerulean'
game 'gta5'

author 'SirenSharp'
description 'In-game siren tester for SirenSharp packs - browse soundsets and play sirens via a RageUI menu, with no LVC/VCF config.'
version '1.0.0'

client_scripts {
    -- Vendored RageUI (pinned). See RageUI/VENDORED.md.
    'RageUI/src/RageUI.lua',
    'RageUI/src/Menu.lua',
    'RageUI/src/MenuController.lua',
    'RageUI/src/components/*.lua',
    'RageUI/src/elements/*.lua',
    'RageUI/src/items/*.lua',

    'config.lua',
    'client.lua',
}
