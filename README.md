# Stance Overhaul Fika

Fika addon (SPT 4.1). Players see each other's Stance Overhaul weapon stances: high ready, low ready, short-stock, active aim, patrol, left shoulder, pistol compress.

Status: **Stage 1 (players) implemented, untested in-game.** Stage 2 (bots) not started. Plan: `llm-root/docs/stance-overhaul-fika-sync-plan.md`.

## Install

Everyone in the raid **and the headless host** needs it:

```
BepInEx/plugins/kobethuy-StanceOverhaulFika/StanceOverhaulFika.dll
```

- Needs Fika (`com.fika.core`).
- Stance Overhaul optional. Without it: you still *see* others' stances and the headless host relays them. You just send none.
- Fika errors on a packet it does not know. A peer **without** this mod makes the host log a parse error for each stance packet. Add the mod GUID to Fika's `client.mods.required` list.

## How it works

1. Your Stance Overhaul computes a gun offset (position + rotation). This mod reads it, ~20x/s while moving.
2. Packet (18 bytes) goes through Fika. Client → host → every other client. Host/headless → everyone.
3. Receiver smooths it and adds it to that player's weapon spring rest position (Harmony postfix on `ProceduralWeaponAnimation.ZeroAdjustments`). Same spring mechanism SO uses locally.

Why this is safe:
- Fika patched: **no**. Stance Overhaul changed: **no**.
- Only PWAs of remote players are touched. Your own gun is untouched.
- Unchanged stance resent once per second (late joiners, lost packets). One reliable packet when a stance settles.
- No stance data for 5 s, no gun in hands, or dead → stance fades out.

## Known limits (Stage 1)

- Only the stance offset is synced. Not synced: per-weapon base offset (Stance Overhaul's weapon-id table), alt-ADS gun-to-camera, elbow/left-hand IK tweaks, stamina/speed effects (those are local game logic anyway).
- Stance Overhaul draws left shoulder, patrol and pistol compress purely as a pose, so they ride the same packet. Vanilla left stance stays handled by Fika.
- Remote gun follows through the hands springs, so it lags a touch behind the sender's own view. Intentional (same easing as local).
- Bots: no stances yet (Stage 2).

## Config (BepInEx/config/com.kobethuy.stance-overhaul-fika.cfg)

| Entry | Default | Meaning |
|---|---|---|
| General / Enabled | true | Send + show. Same on every peer |
| Network / Send rate (Hz) | 20 | Max updates/s while moving |
| Network / Keepalive (seconds) | 1 | Resend unchanged stance |
| Display / Smoothing speed | 25 | Higher = gun follows faster |
| Debug / Verbose log | false | Extra log lines |

## Build

Needs: .NET SDK, SPT 4.1 install (default `F:\SPT_4.1`), Stance Overhaul + Common Lib source repos.

```bash
pwsh tools/build-reference-libs.ps1                    # once: builds SO + Common Lib into lib/ (touches nothing else)
dotnet build src/StanceOverhaulFika -c Release         # also copies DLL to <SptRoot>/BepInEx/plugins/kobethuy-StanceOverhaulFika
dotnet build src/StanceOverhaulFika -c Release -p:SkipDeploy=true   # build only
dotnet build ... -p:SptRoot="F:\SPT_4.1 - Dev"         # other install
dotnet test                                            # 36 tests, no game needed
```

## Layout

```
src/StanceOverhaulFika/
  Core/       pure logic, unit tested (quantisation, send scheduling, smoothing)
  Net/        StancePosePacket + StanceNet (only code touching Fika networking)
  Sender/     StanceSource (only code touching Stance Overhaul) + StanceSender
  Receiver/   RemoteStance(s): per-player state, smoothing, PWA mapping
  Patches/    ZeroAdjustmentsPatch: applies the pose
tests/        xunit
tools/        build-reference-libs.ps1
```

## In-game test checklist

Stage 1. Mark each when verified.

- [ ] Solo raid, no Fika: Stance Overhaul unchanged
- [ ] Host + 1 client: each sees other's high ready / low ready / short-stock / active aim / patrol / left shoulder
- [ ] Host + 2 clients (client A sees client B: relay through host)
- [ ] Headless + 2 clients
- [ ] Enter, exit, switch A→B mid-blend look same as local
- [ ] ADS, reload, weapon swap, sprint/tac sprint, prone/crouch: no stuck or twisted gun
- [ ] Knife / grenade / meds in hands: no stance applied
- [ ] Join mid-raid: existing players' stances appear within ~1 s
- [ ] Death: stance fades, no errors
- [ ] Client without Stance Overhaul: sees others' stances
- [ ] Log: `stance packet registered` on every peer, no parse errors
- [ ] Two raids in a row (re-registration)
