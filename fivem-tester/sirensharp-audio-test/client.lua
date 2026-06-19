--[[
    SirenSharp in-game tester
    -------------------------
    Browse the soundsets from config.lua and play/stop their sirens through a
    RageUI menu. Plays positionally from the vehicle you're in (or your ped),
    with a 2D frontend fallback. No LVC / VCF config required - this isolates
    "does the SirenSharp pack actually work in-game" from server-side audio config.

    Sounds are played with the wavepack the dat54 registered:
      PlaySoundFromEntity(soundId, '<siren>', entity, '<soundset>_soundset', 0, 0)
]]

local MainMenu = RageUI.CreateMenu('SirenSharp', 'Audio Test')
MainMenu.EnableMouse = true

-- One submenu per configured soundset, keyed by soundset name.
local SoundsetMenus = {}
for _, soundset in ipairs(Config.Soundsets) do
    SoundsetMenus[soundset.name] = RageUI.CreateSubMenu(MainMenu, 'SirenSharp', soundset.name)
end

local use2D = false
local currentSoundId = nil
local currentLabel = nil

local function echo(msg)
    print(('[sirensharp-audio-test] %s'):format(msg))
end

local function stopCurrent()
    if currentSoundId ~= nil then
        StopSound(currentSoundId)
        ReleaseSoundId(currentSoundId)
        echo(('stopped %s'):format(currentLabel or '?'))
        currentSoundId = nil
        currentLabel = nil
    end
end

local function playSiren(soundset, siren, dlc)
    stopCurrent()

    local ped = PlayerPedId()
    local veh = GetVehiclePedIsIn(ped, false)
    local entity = (veh ~= 0) and veh or ped
    local setName = ('%s_soundset'):format(soundset)
    local bank = ('dlc_%s/%s'):format(dlc, soundset)

    currentSoundId = GetSoundId()
    currentLabel = ('%s / %s'):format(setName, siren)

    if use2D then
        -- 2D fallback: audible regardless of position, useful to confirm the
        -- bank loaded even if positional playback seems silent.
        PlaySoundFrontend(currentSoundId, siren, setName, true)
    else
        PlaySoundFromEntity(currentSoundId, siren, entity, setName, 0, 0)
    end

    -- Echo the exact native names + Bank path to F8 so the player can compare
    -- against their LVC VCF (String/Ref = siren name, Bank = the dlc path).
    echo(('play name="%s" soundset="%s" bank="%s" mode=%s entity=%d')
        :format(siren, setName, bank, use2D and '2D' or 'entity', entity))
end

-- Release the handle once a one-shot finishes (sirens loop, so this mostly
-- applies to horns); keeps GetSoundId handles from leaking.
CreateThread(function()
    while true do
        if currentSoundId ~= nil and HasSoundFinished(currentSoundId) then
            ReleaseSoundId(currentSoundId)
            currentSoundId = nil
            currentLabel = nil
        end
        Wait(500)
    end
end)

function RageUI.PoolMenus:SirenTest()
    MainMenu:IsVisible(function(Items)
        Items:CheckBox('2D (frontend) mode', 'Play through PlaySoundFrontend instead of from your vehicle/ped.', use2D, {}, function(onSelected, IsChecked)
            if onSelected then use2D = IsChecked end
        end)
        Items:AddButton('Stop all', 'Stop the sound currently playing.', {}, function(onSelected)
            if onSelected then stopCurrent() end
        end)
        Items:AddSeparator('Soundsets')
        for _, soundset in ipairs(Config.Soundsets) do
            Items:AddButton(soundset.name, ('Bank: dlc_%s/%s'):format(soundset.dlc, soundset.name), {}, function() end, SoundsetMenus[soundset.name])
        end
    end, function(Panels) end)

    for _, soundset in ipairs(Config.Soundsets) do
        SoundsetMenus[soundset.name]:IsVisible(function(Items)
            for _, siren in ipairs(soundset.sirens) do
                Items:AddButton(siren, ("name '%s' in '%s_soundset'"):format(siren, soundset.name), {}, function(onSelected)
                    if onSelected then playSiren(soundset.name, siren, soundset.dlc) end
                end)
            end
            Items:AddSeparator('---')
            Items:AddButton('Stop', 'Stop the sound currently playing.', {}, function(onSelected)
                if onSelected then stopCurrent() end
            end)
        end, function(Panels) end)
    end
end

local function toggleMenu()
    RageUI.Visible(MainMenu, not RageUI.Visible(MainMenu))
end

RegisterCommand(Config.Command, function()
    toggleMenu()
end, false)

if Config.Keybind ~= nil and Config.Keybind ~= '' then
    RegisterKeyMapping(Config.Command, 'Open SirenSharp audio test', 'keyboard', Config.Keybind)
end

CreateThread(function()
    if #Config.Soundsets == 0 then
        echo('config.lua has no soundsets - edit it (or regenerate from SirenSharp) before using /' .. Config.Command)
    else
        echo(('ready - /%s to open the menu (%d soundset(s))'):format(Config.Command, #Config.Soundsets))
    end
end)
