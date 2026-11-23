import math
import struct
import wave
import os

SAMPLE_RATE = 44100

def write_wav(filename, samples, sample_rate=SAMPLE_RATE):
    # Ensure directory exists
    os.makedirs(os.path.dirname(os.path.abspath(filename)), exist_ok=True)
    with wave.open(filename, 'w') as wav_file:
        wav_file.setnchannels(2)  # Stereo
        wav_file.setsampwidth(2)  # 16-bit
        wav_file.setframerate(sample_rate)
        
        # samples should be list of (left, right) floats between -1.0 and 1.0
        packed_data = bytearray()
        for left, right in samples:
            l_clamped = max(-1.0, min(1.0, left))
            r_clamped = max(-1.0, min(1.0, right))
            l_int = int(l_clamped * 32767)
            r_int = int(r_clamped * 32767)
            packed_data.extend(struct.pack('<hh', l_int, r_int))
        wav_file.writeframes(packed_data)
    print(f"Written: {filename} ({len(samples)/sample_rate:.1f}s)")

def make_loopable(samples, loop_xfade_len):
    """Seamless loop crossfade at the ends"""
    n = len(samples)
    out = list(samples)
    for i in range(loop_xfade_len):
        t = i / loop_xfade_len
        # Crossfade end into beginning
        # out[i] fades in from out[n - loop_xfade_len + i]
        l1, r1 = samples[i]
        l2, r2 = samples[n - loop_xfade_len + i]
        out[i] = (l1 * t + l2 * (1.0 - t), r1 * t + r2 * (1.0 - t))
    # Return without the tail overlap
    return out[:n - loop_xfade_len]

def generate_pad_tone(freq, t, detune=1.002):
    # Smooth warm sine + soft 2nd & 3rd harmonic
    v1 = math.sin(2.0 * math.pi * freq * t)
    v2 = 0.4 * math.sin(2.0 * math.pi * freq * detune * t)
    v3 = 0.15 * math.sin(2.0 * math.pi * freq * 2.0 * t)
    v4 = 0.08 * math.sin(2.0 * math.pi * freq * 3.0 * t)
    return (v1 + v2 + v3 + v4) * 0.6

# 1. Level 1: Calm Forest & Green Meadow (Peaceful, gentle, soothing strings & pads)
def generate_forest_ambient(duration=16.0):
    total_samples = int(duration * SAMPLE_RATE)
    # Chord progression: Cmaj9 -> Am9 -> Fmaj9 -> G6 (each 4 seconds)
    chords = [
        [130.81, 196.00, 246.94, 293.66, 329.63], # C3, G3, B3, D4, E4
        [110.00, 164.81, 220.00, 261.63, 329.63], # A2, E3, A3, C4, E4
        [87.31, 174.61, 220.00, 261.63, 349.23],  # F2, F3, A3, C4, F4
        [98.00, 146.83, 196.00, 246.94, 293.66],  # G2, D3, G3, B3, D4
    ]
    chord_len = duration / len(chords)
    
    samples = []
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        chord_idx = int(t / chord_len) % len(chords)
        chord_t = (t % chord_len)
        
        # Smooth envelope for each chord transition
        env = math.sin(math.pi * (chord_t / chord_len)) ** 1.5
        
        # Gentle stereo spread
        left = 0.0
        right = 0.0
        cur_chord = chords[chord_idx]
        for note_idx, freq in enumerate(cur_chord):
            pan = (note_idx / (len(cur_chord) - 1)) * 0.6 + 0.2 # 0.2 to 0.8
            # subtle vibrato
            vib = 1.0 + 0.003 * math.sin(2.0 * math.pi * 3.5 * t)
            sig = generate_pad_tone(freq * vib, t) * env
            left += sig * (1.0 - pan)
            right += sig * pan
            
        # Subtle gentle high chime
        chime_t = t % 2.0
        if chime_t < 1.0:
            chime_env = math.exp(-chime_t * 3.5)
            chime_freq = 523.25 * (1.0 if (int(t/2) % 2 == 0) else 1.25)
            chime_sig = 0.08 * math.sin(2.0 * math.pi * chime_freq * t) * chime_env
            left += chime_sig * 0.6
            right += chime_sig * 0.4
            
        # Overall gentle master volume
        samples.append((left * 0.22, right * 0.22))
        
    return make_loopable(samples, int(1.5 * SAMPLE_RATE))

# 2. Level 2: Calm Desert (Warm, meditative sunset ambient drone)
def generate_desert_ambient(duration=16.0):
    total_samples = int(duration * SAMPLE_RATE)
    # Warm D minor / D Dorian chords: Dm9 -> G/D -> Bbmaj7 -> Dsus2
    chords = [
        [73.42, 146.83, 220.00, 261.63, 329.63], # D2, D3, A3, C4, E4
        [73.42, 146.83, 196.00, 246.94, 293.66], # D2, D3, G3, B3, D4
        [58.27, 116.54, 174.61, 220.00, 293.66], # Bb1, Bb2, F3, A3, D4
        [73.42, 146.83, 220.00, 293.66, 329.63], # D2, D3, A3, D4, E4
    ]
    chord_len = duration / len(chords)
    
    samples = []
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        chord_idx = int(t / chord_len) % len(chords)
        chord_t = (t % chord_len)
        env = math.sin(math.pi * (chord_t / chord_len)) ** 1.4
        
        left = 0.0
        right = 0.0
        cur_chord = chords[chord_idx]
        for note_idx, freq in enumerate(cur_chord):
            pan = (note_idx / (len(cur_chord) - 1)) * 0.5 + 0.25
            vib = 1.0 + 0.002 * math.sin(2.0 * math.pi * 2.8 * t)
            sig = generate_pad_tone(freq * vib, t, detune=1.0015) * env
            left += sig * (1.0 - pan)
            right += sig * pan
            
        # Warm subtle sub-drone
        sub = 0.15 * math.sin(2.0 * math.pi * 73.42 * t)
        left += sub
        right += sub
        
        samples.append((left * 0.22, right * 0.22))
        
    return make_loopable(samples, int(1.5 * SAMPLE_RATE))

# 3. Level 3: Calm Winter / Ice (Serene crystalline ambient, peaceful and soft)
def generate_ice_ambient(duration=16.0):
    total_samples = int(duration * SAMPLE_RATE)
    # Ethereal Fmaj9 -> Cmaj7 -> Dm7 -> Am
    chords = [
        [87.31, 174.61, 261.63, 329.63, 392.00], # F2, F3, C4, E4, G4
        [65.41, 130.81, 196.00, 246.94, 329.63], # C2, C3, G3, B3, E4
        [73.42, 146.83, 220.00, 261.63, 349.23], # D2, D3, A3, C4, F4
        [55.00, 110.00, 164.81, 220.00, 329.63], # A1, A2, E3, A3, E4
    ]
    chord_len = duration / len(chords)
    
    samples = []
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        chord_idx = int(t / chord_len) % len(chords)
        chord_t = (t % chord_len)
        env = math.sin(math.pi * (chord_t / chord_len)) ** 1.3
        
        left = 0.0
        right = 0.0
        cur_chord = chords[chord_idx]
        for note_idx, freq in enumerate(cur_chord):
            pan = (note_idx / (len(cur_chord) - 1)) * 0.6 + 0.2
            sig = generate_pad_tone(freq, t, detune=1.003) * env
            left += sig * (1.0 - pan)
            right += sig * pan
            
        # Soft crystalline high sparkle (gentle sine overtone)
        sparkle = 0.05 * math.sin(2.0 * math.pi * 1046.5 * t) * (0.5 + 0.5 * math.sin(2.0 * math.pi * 0.5 * t))
        left += sparkle * 0.7
        right += sparkle * 0.3
        
        samples.append((left * 0.22, right * 0.22))
        
    return make_loopable(samples, int(1.5 * SAMPLE_RATE))

# 4. Level 4: Calm Lava Caldera (Deep, warm, soothing subterranean ambient pad)
def generate_lava_calm_ambient(duration=16.0):
    total_samples = int(duration * SAMPLE_RATE)
    # Warm, relaxing deep pads: C minor 9 -> Ab maj7 -> Eb maj7 -> Bb sus2
    chords = [
        [65.41, 130.81, 196.00, 233.08, 311.13], # C2, C3, G3, Bb3, Eb4
        [51.91, 103.83, 155.56, 207.65, 261.63], # Ab1, Ab2, Eb3, Ab3, C4
        [77.78, 155.56, 233.08, 293.66, 349.23], # Eb2, Eb3, Bb3, D4, F4
        [58.27, 116.54, 174.61, 233.08, 261.63], # Bb1, Bb2, F3, Bb3, C4
    ]
    chord_len = duration / len(chords)
    
    samples = []
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        chord_idx = int(t / chord_len) % len(chords)
        chord_t = (t % chord_len)
        env = math.sin(math.pi * (chord_t / chord_len)) ** 1.35
        
        left = 0.0
        right = 0.0
        cur_chord = chords[chord_idx]
        for note_idx, freq in enumerate(cur_chord):
            pan = (note_idx / (len(cur_chord) - 1)) * 0.5 + 0.25
            vib = 1.0 + 0.0015 * math.sin(2.0 * math.pi * 1.8 * t)
            sig = generate_pad_tone(freq * vib, t, detune=1.002) * env
            left += sig * (1.0 - pan)
            right += sig * pan
            
        # Warm, soothing deep rumble (pure low sub-sine ~45Hz - 65Hz)
        warm_sub = 0.18 * math.sin(2.0 * math.pi * 55.0 * t) * (0.8 + 0.2 * math.sin(2.0 * math.pi * 0.25 * t))
        left += warm_sub
        right += warm_sub
        
        samples.append((left * 0.22, right * 0.22))
        
    return make_loopable(samples, int(1.5 * SAMPLE_RATE))

# 5. Ambient: Smooth gentle atmospheric air/wind
def generate_calm_ambient_wind(duration=12.0):
    total_samples = int(duration * SAMPLE_RATE)
    samples = []
    
    # Generate pink/filtered noise with gentle breathing envelope
    state_l = 0.0
    state_r = 0.0
    
    import random
    random.seed(42)
    
    for i in range(total_samples):
        t = i / SAMPLE_RATE
        # Gentle swell (slow breathing cycle every 6s)
        swell = 0.6 + 0.4 * math.sin(2.0 * math.pi * (t / 6.0))
        
        white_l = (random.random() * 2.0 - 1.0)
        white_r = (random.random() * 2.0 - 1.0)
        
        # Heavy low-pass filter (cutoff ~250 Hz) for soft calm warmth
        alpha = 0.035
        state_l += alpha * (white_l - state_l)
        state_r += alpha * (white_r - state_r)
        
        # Add very gentle warm harmonic undertone (110 Hz)
        hum = 0.03 * math.sin(2.0 * math.pi * 110.0 * t)
        
        out_l = (state_l * 2.5 + hum) * swell * 0.12
        out_r = (state_r * 2.5 + hum) * swell * 0.12
        samples.append((out_l, out_r))
        
    return make_loopable(samples, int(1.5 * SAMPLE_RATE))

def main():
    tracks = [
        ("BGMusic1.wav", generate_forest_ambient()),
        ("BGMusic2.wav", generate_desert_ambient()),
        ("BGMusic3.wav", generate_ice_ambient()),
        ("BGMusic4.wav", generate_lava_calm_ambient()),
        ("Ambient.wav",  generate_calm_ambient_wind()),
    ]
    
    destinations = [
        "h:/32bit/Assets/Audio/",
        "h:/32bit/Assets/Resources/Audio/"
    ]
    
    for filename, samples in tracks:
        for dest in destinations:
            full_path = os.path.join(dest, filename)
            write_wav(full_path, samples)
            
    print("All calm audio tracks generated successfully!")

if __name__ == "__main__":
    main()
