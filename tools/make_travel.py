#!/usr/bin/env python3
"""
Synthesizes the looping bass line played while travelling through a portal (package/sounds/travel.wav).

An original two-bar ostinato in the key of E: four bass notes on the IV chord (A), then four on the I chord (E),
alternating, in a driving eighth-note pulse. Plain synthesis (saw + square + sub, plucked filter): no samples,
nothing taken from any recording.

Usage:  python3 -I tools/make_travel.py package/sounds/travel.wav [--bpm 138]
"""
import argparse
import math
import struct
import wave

RATE = 44100

# Root notes (Hz): the IV chord (A2) then the I chord (E2), each played four times (the pattern is the octave
# multiplier for each of the four notes: root, root, octave up, root).
CHORDS = [110.00, 82.41]
PATTERN = [1, 1, 2, 1]


def synth_note(buf, start, freq, duration, accent):
    """Adds one plucked bass note to buf, wrapping past the end so the loop is seamless."""
    n = len(buf)
    samples = int(duration * RATE)
    phase = phase_sub = 0.0
    low = 0.0
    for i in range(samples):
        t = i / RATE
        phase = (phase + freq / RATE) % 1.0
        phase_sub = (phase_sub + freq / 2 / RATE) % 1.0
        saw = 2.0 * phase - 1.0
        square = 1.0 if phase < 0.5 else -1.0
        sub = math.sin(2.0 * math.pi * phase_sub)
        raw = 0.55 * saw + 0.22 * square + 0.60 * sub

        # A plucked filter: bright at the start of each note, closing quickly (gives it a "bwow" punch).
        cutoff = 420.0 + 1900.0 * math.exp(-t / 0.055)
        a = 1.0 - math.exp(-2.0 * math.pi * cutoff / RATE)
        low += a * (raw - low)

        # Quick attack, short release, a little extra weight on the first note of each chord.
        attack = min(1.0, t / 0.004)
        release = min(1.0, (duration - t) / 0.035)
        amp = attack * release * (1.15 if accent else 1.0)
        buf[(start + i) % n] += low * amp


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("--bpm", type=float, default=138.0)
    args = ap.parse_args()

    note = 60.0 / args.bpm / 2.0                 # one eighth note
    notes_per_loop = len(CHORDS) * len(PATTERN)
    length = int(round(note * notes_per_loop * RATE))
    buf = [0.0] * length

    for n in range(notes_per_loop):
        chord, step = divmod(n, len(PATTERN))
        synth_note(buf, int(round(n * note * RATE)), CHORDS[chord] * PATTERN[step], note * 0.93, accent=(step == 0))

    peak = max(abs(x) for x in buf)
    gain = 0.70 / peak                            # about -3 dB
    with wave.open(args.out, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, x * gain)) * 32767)) for x in buf))
    print(f"{args.out}: {length / RATE:.2f} s loop at {args.bpm:g} bpm ({notes_per_loop} notes)")


if __name__ == "__main__":
    main()
