#!/usr/bin/env python3
"""
Synthesizes the looping bass line played while travelling through a portal (package/sounds/travel.wav).

An original two-bar ostinato in the key of E: eight bass notes on the IV chord (A), then eight on the I chord (E),
alternating, with a low drone underneath that follows the chord. Plain synthesis (saw + square + sub, plucked filter): no samples,
nothing taken from any recording.

Usage:  python3 -I tools/make_travel.py package/sounds/travel.wav [--bpm 116] [--style straight|triplet] [--no-drone]
"""
import argparse
import math
import struct
import wave

RATE = 44100

# Root notes (Hz): the IV chord (A2) then the I chord (E2). Each chord gets eight notes over one bar of 4/4, all on its root.
CHORDS = [110.00, 82.41]

# The drone under the bass: a sustained low note on each chord's root (A1 for the IV chord, E1 for the I chord) that
# glides across when the chord changes. Its frequencies are nudged so every partial fits a whole number of cycles in
# the loop, which keeps the loop seamless even though it's continuous.
DRONE_ROOTS = [55.00, 41.20]
DRONE_LEVEL = 0.50
DRONE_GLIDE = 0.30     # seconds taken to slide from one chord's drone to the next


# A style lists each note of one chord as (length in beats, octave multiplier, accent). Beats are quarter notes.
T = 1.0 / 3.0   # a triplet: three of them fill one beat
STYLES = {
    # Even eighth notes, all on the chord's root (it's a bass line).
    "straight": [(0.5, 1, 1.15 if i == 0 else 1.07 if i == 4 else 1.0) for i in range(8)],

    # A triplet feel, in the spirit of the half-bar rhythm blocks described for the 1963 theme:
    # "diddly-dum" (three quick notes then a quarter note) followed by "dum-dum-diddy" (two longer notes, then two quick).
    "triplet": [
        (T, 1, 1.15), (T, 1, 1.0), (T, 1, 1.0), (1.0, 1, 1.1),          # da-da-da DUM
        (2 * T, 1, 1.07), (2 * T, 1, 1.0), (T, 1, 1.0), (T, 1, 1.0),    # DUM-DUM-da-da
    ],
}


def synth_note(buf, start, freq, duration, accent=1.0):
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
        amp = attack * release * accent
        buf[(start + i) % n] += low * amp


def smoothstep(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3.0 - 2.0 * x)


def add_drone(buf):
    """Mixes in the drone, changing chord at the loop's halfway point and again at the loop point."""
    n = len(buf)
    seconds = n / RATE
    half = n // 2
    glide = DRONE_GLIDE * RATE

    def partial(freq, i):
        cycles = max(1, round(freq * seconds))     # whole cycles per loop: no click at the join
        return math.sin(2.0 * math.pi * cycles * i / n)

    for i in range(n):
        # How much of the first chord (A) is sounding: it takes over around sample 0 and hands over around the middle.
        d0 = ((i + half) % n) - half               # distance from the loop point, wrapping round
        into_a = smoothstep((d0 + glide / 2) / glide)
        into_e = smoothstep((i - half + glide / 2) / glide)
        w_a = into_a * (1.0 - into_e)

        voices = []
        for root in DRONE_ROOTS:
            # The root, a softer octave above, and a slightly detuned copy that slowly beats for warmth.
            voices.append(0.90 * partial(root, i) + 0.30 * partial(root * 2, i) + 0.22 * partial(root * 1.004, i))
        buf[i] += DRONE_LEVEL * (w_a * voices[0] + (1.0 - w_a) * voices[1])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("--bpm", type=float, default=116.0)
    ap.add_argument("--style", choices=sorted(STYLES), default="straight")
    ap.add_argument("--no-drone", action="store_true", help="leave out the drone under the bass")
    args = ap.parse_args()

    beat = 60.0 / args.bpm
    style = STYLES[args.style]
    bar_beats = sum(length for length, _, _ in style)
    length = int(round(beat * bar_beats * len(CHORDS) * RATE))
    buf = [0.0] * length

    position = 0.0  # in beats
    for freq in CHORDS:
        for note_beats, octave, accent in style:
            synth_note(buf, int(round(position * beat * RATE)), freq * octave, note_beats * beat * 0.93, accent)
            position += note_beats

    if not args.no_drone:
        add_drone(buf)

    peak = max(abs(x) for x in buf)
    gain = 0.70 / peak                            # about -3 dB
    with wave.open(args.out, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, x * gain)) * 32767)) for x in buf))
    print(f"{args.out}: {length / RATE:.2f} s loop at {args.bpm:g} bpm, {args.style} style ({len(style) * len(CHORDS)} notes){'' if args.no_drone else ', with drone'}")


if __name__ == "__main__":
    main()
