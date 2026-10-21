#!/usr/bin/env python3
"""
Builds the six gate sounds bundled with PortalHeim_SG1 (package/sounds/*.ogg).

They are mixes of a few CC0 recordings plus sounds synthesized here with ffmpeg. See package/sounds/CREDITS.txt.
Needs ffmpeg. The source recordings aren't kept in the repo; download them first:

  rubberduck, "100 CC0 SFX #2"      https://opengameart.org/content/100-cc0-sfx-2        (sfx_100_v2.zip, unzip it)
  BMacZero,  "Metal Impact Sounds"   https://opengameart.org/content/metal-impact-sounds   (thud3.wav)
  ezwa,      "6 Short Water Splashes" https://opengameart.org/content/6-short-water-splashes (7z, extract it)

Usage:
  python3 -I tools/make_sounds.py --sfx100 DIR --metal DIR --water DIR --out package/sounds
"""
import argparse
import os
import subprocess
import sys
import tempfile

RATE = 44100


def run(cmd):
    result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    if result.returncode != 0:
        sys.exit(f"ffmpeg failed:\n{' '.join(cmd)}\n{result.stderr[-1500:]}")
    return result.stderr


def probe_duration(path):
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
                         stdout=subprocess.PIPE, text=True, check=True).stdout
    return float(out.strip())


def peak_db(path):
    err = run(["ffmpeg", "-hide_banner", "-nostats", "-i", path, "-af", "volumedetect", "-f", "null", "-"])
    for line in err.splitlines():
        if "max_volume" in line:
            return float(line.split("max_volume:")[1].split("dB")[0])
    return 0.0


def finish(mixed_wav, out_ogg, target_peak_db):
    """Scale so the loudest peak sits at target_peak_db, then write mono 16-bit WAV (small clips; exact loops)."""
    gain = target_peak_db - peak_db(mixed_wav)
    run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", mixed_wav, "-af", f"volume={gain:.2f}dB",
         "-ac", "1", "-ar", str(RATE), "-c:a", "pcm_s16le", out_ogg])
    print(f"  {os.path.basename(out_ogg)}: {os.path.getsize(out_ogg) / 1024:.0f} KB, peak {target_peak_db} dB")


def lavfi(expr, duration):
    return ["-f", "lavfi", "-t", str(duration), "-i", f"aevalsrc=exprs='{expr}':d={duration}:s={RATE}"]


def noise(color, duration, amplitude=0.8):
    return ["-f", "lavfi", "-t", str(duration), "-i", f"anoisesrc=color={color}:d={duration}:r={RATE}:amplitude={amplitude}"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sfx100", required=True)
    ap.add_argument("--metal", required=True)
    ap.add_argument("--water", required=True)
    ap.add_argument("--out", required=True)
    a = ap.parse_args()

    sfx = lambda name: os.path.join(a.sfx100, f"sfx100v2_{name}.ogg")
    metal = lambda name: os.path.join(a.metal, name)
    splash = lambda n: os.path.join(a.water, f"water_splash-{n:02d}.flac")
    os.makedirs(a.out, exist_ok=True)
    tmp = tempfile.mkdtemp(prefix="sg1sounds_")
    wav = lambda name: os.path.join(tmp, name + ".wav")
    ffm = ["ffmpeg", "-y", "-hide_banner", "-loglevel", "error"]
    print("Building sounds:")

    # chevrune: a heavy thud with a sharp metal click just behind it.
    run(ffm + ["-i", metal("thud3.wav"), "-i", sfx("lock_open_01"), "-filter_complex",
               "[0]volume=1.0,aresample=44100,pan=mono|c0=c0[a];[1]volume=0.8,aresample=44100,pan=mono|c0=c0,adelay=25[b];"
               "[a][b]amix=inputs=2:normalize=0,atrim=0:0.7,afade=t=out:st=0.55:d=0.15", wav("chevrune")])
    finish(wav("chevrune"), os.path.join(a.out, "chevrune.wav"), -3)

    # spin: a stone scrape over a low grinding rumble (brown noise).
    run(ffm + ["-i", sfx("stones_02")] + noise("brown", 0.55, 0.9) + [
        "-filter_complex",
        "[0]aresample=44100,pan=mono|c0=c0,volume=0.7[a];"
        "[1]lowpass=f=420,afade=t=in:d=0.08,afade=t=out:st=0.33:d=0.22,volume=2.2[b];"
        "[a][b]amix=inputs=2:normalize=0,atrim=0:0.55", wav("spin")])
    finish(wav("spin"), os.path.join(a.out, "spin.wav"), -4)

    # activate: a deep whoomph (synth), a rolling rumble (thunder), and a big splash.
    run(ffm + lavfi("0.9*sin(2*PI*t*(35+45*t))*exp(-1.8*t)", 2.2) + ["-i", sfx("thunder_01"), "-i", splash(4),
               "-filter_complex",
               "[0]volume=1.0[w];[1]aresample=44100,pan=mono|c0=c0,atrim=0:2.2,afade=t=in:d=0.05,afade=t=out:st=1.0:d=1.2,volume=0.7[t];"
               "[2]aresample=44100,pan=mono|c0=c0,adelay=60,volume=0.8[s];"
               "[w][t][s]amix=inputs=3:normalize=0,atrim=0:2.2", wav("activate")])
    finish(wav("activate"), os.path.join(a.out, "activate.wav"), -3)

    # idle: a seamless loop. The CC0 drone loop plus a low hum whose cycles fit the loop length exactly.
    drone = sfx("loop_ambient_03")
    dur = probe_duration(drone)
    f1, f2, f3 = (round(f * dur) / dur for f in (55.0, 110.0, 165.0))
    hum = f"0.5*sin(2*PI*{f1:.6f}*t)+0.28*sin(2*PI*{f2:.6f}*t)+0.14*sin(2*PI*{f3:.6f}*t)"
    run(ffm + ["-i", drone] + lavfi(hum, f"{dur:.6f}") + ["-filter_complex",
               "[0]aresample=44100,pan=mono|c0=c0,volume=0.9[d];[1]volume=0.6[h];[d][h]amix=inputs=2:normalize=0", wav("idle")])
    finish(wav("idle"), os.path.join(a.out, "idle.wav"), -9)

    # teleport: a whoosh (shaped noise) with a splash and a low thump as you pass through.
    run(ffm + noise("pink", 1.3, 0.9) + ["-i", splash(2)] + lavfi("0.9*sin(2*PI*t*(120-90*t))*exp(-6*t)", 0.4) + [
        "-filter_complex",
        "[0]highpass=f=180,lowpass=f=3200,aphaser=speed=0.5:decay=0.6,afade=t=in:d=0.55:curve=qsin,afade=t=out:st=0.6:d=0.7[w];"
        "[1]aresample=44100,pan=mono|c0=c0,adelay=380,volume=0.7[s];[2]adelay=420,volume=0.9[b];"
        "[w][s][b]amix=inputs=3:normalize=0,atrim=0:1.3", wav("teleport")])
    finish(wav("teleport"), os.path.join(a.out, "teleport.wav"), -4)

    # close: a power-down sweep falling away, with a heavy thud.
    run(ffm + lavfi("0.9*sin(2*PI*t*(200-110*t))*exp(-2.2*t)", 1.6) + ["-i", metal("thud2.wav"), "-filter_complex",
               "[0]volume=1.0[s];[1]aresample=44100,pan=mono|c0=c0,volume=0.9[t];[s][t]amix=inputs=2:normalize=0,atrim=0:1.6,afade=t=out:st=1.2:d=0.4",
               wav("close")])
    finish(wav("close"), os.path.join(a.out, "close.wav"), -3)


if __name__ == "__main__":
    main()
