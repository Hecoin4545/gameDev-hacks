import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

def create_level_buttons():
    width = 460
    height = 130
    radius = 26

    # Try to load a clean sans-serif font
    font_candidates = [
        "C:/Windows/Fonts/arialbd.ttf",
        "C:/Windows/Fonts/segoeuib.ttf",
        "C:/Windows/Fonts/trebucbd.ttf",
        "C:/Windows/Fonts/verdanab.ttf"
    ]
    title_font = None
    sub_font = None
    for f in font_candidates:
        try:
            title_font = ImageFont.truetype(f, 44)
            sub_font = ImageFont.truetype(f, 18)
            small_font = ImageFont.truetype(f, 16)
            break
        except Exception:
            continue
    if title_font is None:
        title_font = ImageFont.load_default()
        sub_font = title_font
        small_font = title_font

    level_meta = {
        1: ("LEVEL 1", "PATH FINDER", True),
        2: ("LEVEL 2", "MEMORY RUN", True),
        3: ("LEVEL 3", "INVERTED RUN", True),
        4: ("LEVEL 4", "STAGE 04", False),
        5: ("LEVEL 5", "STAGE 05", False),
        6: ("LEVEL 6", "STAGE 06", False),
        7: ("LEVEL 7", "STAGE 07", False),
        8: ("LEVEL 8", "STAGE 08", False),
        9: ("LEVEL 9", "STAGE 09", False),
        10: ("LEVEL 10", "FINAL STAGE", False),
    }

    for lvl in range(1, 11):
        title, sub, unlocked = level_meta[lvl]

        # 4x super-sampling for anti-aliased bevels
        scale = 3
        sw = width * scale
        sh = height * scale
        sr = radius * scale

        img = Image.new("RGBA", (sw, sh), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        # Base colors: Emerald green matching Level 1 & 2 grass
        if unlocked:
            c_top = (74, 222, 128)      # #4ADE80
            c_mid = (22, 163, 74)       # #16A34A
            c_bot = (20, 83, 45)        # #14532D
            border_outer = (245, 158, 11) # Gold #F59E0B
            border_inner = (254, 240, 138) # Light gold highlight
            glow_col = (34, 197, 94, 180)
        else:
            c_top = (75, 95, 85)
            c_mid = (45, 62, 54)
            c_bot = (25, 38, 32)
            border_outer = (120, 115, 100)
            border_inner = (160, 155, 140)
            glow_col = (50, 70, 60, 120)

        # Draw outer shadow / border (gold bevel)
        bw = 7 * scale
        draw.rounded_rectangle([0, 0, sw - 1, sh - 1], radius=sr, fill=border_outer)

        # Inner highlight border
        draw.rounded_rectangle([2 * scale, 2 * scale, sw - 1 - 2 * scale, sh - 1 - 2 * scale],
                               radius=sr - 2 * scale, fill=border_inner)

        # Inner dark rim
        draw.rounded_rectangle([bw, bw, sw - 1 - bw, sh - 1 - bw],
                               radius=sr - bw, fill=c_bot)

        # Vertical gradient inside
        inner_box = (bw + 2 * scale, bw + 2 * scale, sw - 1 - bw - 2 * scale, sh - 1 - bw - 2 * scale)
        ix0, iy0, ix1, iy1 = inner_box
        ih = iy1 - iy0
        mask = Image.new("L", (sw, sh), 0)
        mdraw = ImageDraw.Draw(mask)
        mdraw.rounded_rectangle(inner_box, radius=sr - bw - 2 * scale, fill=255)

        grad = Image.new("RGBA", (sw, sh), (0, 0, 0, 0))
        gdraw = ImageDraw.Draw(grad)
        for y in range(int(iy0), int(iy1) + 1):
            t = (y - iy0) / max(1, ih)
            if t < 0.5:
                # Top to mid
                k = t * 2.0
                r = int(c_top[0] * (1 - k) + c_mid[0] * k)
                g = int(c_top[1] * (1 - k) + c_mid[1] * k)
                b = int(c_top[2] * (1 - k) + c_mid[2] * k)
            else:
                # Mid to bot
                k = (t - 0.5) * 2.0
                r = int(c_mid[0] * (1 - k) + c_bot[0] * k)
                g = int(c_mid[1] * (1 - k) + c_bot[1] * k)
                b = int(c_mid[2] * (1 - k) + c_bot[2] * k)
            gdraw.line([(ix0, y), (ix1, y)], fill=(r, g, b, 255))

        # Top gloss shine
        gloss_h = int(ih * 0.42)
        for y in range(int(iy0), int(iy0) + gloss_h):
            t = (y - iy0) / gloss_h
            alpha = int((1.0 - t * 0.7) * 75)
            gdraw.line([(ix0, y), (ix1, y)], fill=(255, 255, 255, alpha))

        img.paste(grad, (0, 0), mask)

        # Scale down with Lanczos for silky smooth anti-aliased graphics
        final_btn = img.resize((width, height), Image.Resampling.LANCZOS)
        fdraw = ImageDraw.Draw(final_btn)

        # Draw level icon on the left
        icon_cx = 64
        icon_cy = height // 2

        if unlocked:
            # Golden circular badge on the left
            badge_r = 28
            fdraw.ellipse([icon_cx - badge_r, icon_cy - badge_r, icon_cx + badge_r, icon_cy + badge_r],
                          fill=(245, 158, 11), outline=(254, 240, 138), width=2)
            fdraw.ellipse([icon_cx - badge_r + 3, icon_cy - badge_r + 3, icon_cx + badge_r - 3, icon_cy + badge_r - 3],
                          fill=(217, 119, 6))

            if lvl == 1:
                # Stepping stones / path symbol
                pts = [(icon_cx - 12, icon_cy + 8), (icon_cx - 4, icon_cy), (icon_cx + 4, icon_cy + 6), (icon_cx + 12, icon_cy - 8)]
                for p in pts:
                    fdraw.ellipse([p[0] - 4, p[1] - 4, p[0] + 4, p[1] + 4], fill=(255, 255, 255))
                fdraw.line(pts, fill=(254, 240, 138), width=3)
            elif lvl == 2:
                # Stopwatch / memory icon
                fdraw.ellipse([icon_cx - 14, icon_cy - 12, icon_cx + 14, icon_cy + 16], outline=(255, 255, 255), width=3)
                fdraw.rectangle([icon_cx - 3, icon_cy - 18, icon_cx + 3, icon_cy - 13], fill=(255, 255, 255))
                fdraw.line([(icon_cx, icon_cy + 2), (icon_cx + 6, icon_cy - 4)], fill=(254, 240, 138), width=3)
            elif lvl == 3:
                # Inverted dual arrows (flip / upside down pattern)
                # Up arrow
                fdraw.polygon([(icon_cx - 7, icon_cy - 2), (icon_cx, icon_cy - 12), (icon_cx + 7, icon_cy - 2)], fill=(255, 255, 255))
                fdraw.rectangle([icon_cx - 3, icon_cy - 3, icon_cx + 3, icon_cy + 3], fill=(255, 255, 255))
                # Down arrow
                fdraw.polygon([(icon_cx - 7, icon_cy + 6), (icon_cx, icon_cy + 16), (icon_cx + 7, icon_cy + 6)], fill=(254, 240, 138))
                fdraw.rectangle([icon_cx - 3, icon_cy + 1, icon_cx + 3, icon_cy + 7], fill=(254, 240, 138))
        else:
            # Locked badge
            badge_r = 26
            fdraw.ellipse([icon_cx - badge_r, icon_cy - badge_r, icon_cx + badge_r, icon_cy + badge_r],
                          fill=(40, 52, 45), outline=(100, 115, 105), width=2)
            # Padlock body
            fdraw.rectangle([icon_cx - 10, icon_cy - 2, icon_cx + 10, icon_cy + 14], fill=(180, 175, 160), outline=(100, 95, 85), width=1)
            # Padlock shackle
            fdraw.arc([icon_cx - 7, icon_cy - 15, icon_cx + 7, icon_cy + 1], start=180, end=0, fill=(210, 205, 190), width=3)

        # Draw Title Text ("LEVEL X")
        tx = 115
        ty = 22

        # Text shadow
        fdraw.text((tx + 2, ty + 2), title, font=title_font, fill=(10, 35, 18, 220))
        # Text main
        title_color = (255, 255, 255) if unlocked else (180, 190, 185)
        fdraw.text((tx, ty), title, font=title_font, fill=title_color)

        # Draw Subtitle Text ("PATH FINDER", "MEMORY RUN", "INVERTED RUN", "LOCKED")
        sy = 74
        sub_color = (254, 240, 138) if unlocked else (140, 150, 145)
        fdraw.text((tx + 1, sy + 1), sub, font=sub_font, fill=(10, 30, 15, 180))
        fdraw.text((tx, sy), sub, font=sub_font, fill=sub_color)

        if not unlocked:
            # "LOCKED" tag on the right
            lock_text = "LOCKED"
            bbox = small_font.getbbox(lock_text)
            lw = bbox[2] - bbox[0]
            lx = width - lw - 24
            ly = height // 2 - 9
            fdraw.rounded_rectangle([lx - 10, ly - 4, lx + lw + 10, ly + 22], radius=6, fill=(30, 42, 35), outline=(80, 95, 85), width=1)
            fdraw.text((lx, ly), lock_text, font=small_font, fill=(245, 158, 11))

        out_path = f"Assets/UI/LevelSelect/Btn_Level{lvl}.png"
        final_btn.save(out_path, "PNG")
        print(f"Saved {out_path}")

create_level_buttons()
