package rmse

import "core:fmt"
import "core:log"
import "core:math"
import "core:os"
import "core:strings"
import "core:strconv"
import rl "vendor:raylib"

Region :: struct
{
    // top left corner
    x: int,
    y: int,
}

RWIDTH :: 128
RHEIGHT :: 128

R1S1 :: Region{639, 237}
R2S1 :: Region{1072, 374}
R3S1 :: Region{749, 480}
R4S1 :: Region{1118, 679}

R1S2 :: Region{1050, 327}

normalize_u8_pixel :: proc(pixel: rl.Color) -> [3]f32
{
    r: f32 = f32(pixel.r) / 255.0
    g: f32 = f32(pixel.g) / 255.0
    b: f32 = f32(pixel.b) / 255.0
    return [3]f32{r, g, b}
}

get_region_by_id :: proc(region_id: int) -> Region
{
    switch region_id
    {
        case 1: return R1S1
        case 2: return R2S1
        case 3: return R3S1
        case 4: return R4S1
        case 5: return R1S2
    }
    log.errorf("Invalid region id %d", region_id)
    return Region{0, 0}
}

is_pixel_in_region :: proc(x, y, region_id: int) -> bool
{
    region := get_region_by_id(region_id)
    if x >= region.x && x < region.x + RWIDTH && y >= region.y && y < region.y + RHEIGHT
    {
        return true
    }
    return false
}

calculate_rmse :: proc(predictions: [^]rl.Color, observation: [^]rl.Color, width, height, region_id: int) -> f32
{
    sum: f32 = 0.0
    n := 0
    for y: = 0; y < height; y += 1
    {
        for x: = 0; x < width; x += 1
        {
            if (region_id != 0)
            {
                if !is_pixel_in_region(x, y, region_id)
                {
                    // pixel is not within the region, so skip calculate RMSE
                    continue
                }
            }

            pxy := predictions[y * width + x]
            oxy := observation[y * width + x]

            // normalize the pixel values to [0, 1]
            npxy: [3]f32 = normalize_u8_pixel(pxy)
            noxy: [3]f32 = normalize_u8_pixel(oxy)

            // textures are in R8 format
            diff := npxy.r - noxy.r
            diff_squared := diff * diff

            sum += diff_squared
            n += 1
        }
    }

    mse: f32 = sum / f32(n)
    return math.sqrt_f32(mse)
}

read_files :: proc (directory: string) -> (fi: []os.File_Info, err: os.Error)
{
    handle: os.Handle
    handle, err = os.open(directory)
    if err != nil
    {
        return nil, err
    }
    defer os.close(handle)
    
    fi, err = os.read_dir(handle, -1)
    if err != nil
    {
        return nil, err
    }

    return fi, nil
}

read_image :: proc (filename: string) -> (image: rl.Image, ok: bool)
{
    filename_cstr := strings.clone_to_cstring(filename)
    image = rl.LoadImage(filename_cstr)
    if image.data == nil
    {
        return image, false
    }
    return image, true
}

load_data :: proc(fi: []os.File_Info) -> (observations: [32]rl.Image, prediction: rl.Image)
{
    for i := 0; i < len(fi); i += 1
    {
        if fi[i].is_dir || strings.contains(fi[i].name, ".png") == false   
        {
            continue
        }
        image, ok := read_image(fi[i].fullpath)
        if !ok
        {
            log.errorf("Error loading image %s", fi[i].fullpath)
            continue
        }
        if strings.contains(fi[i].name, "GT")
        {
            prediction = image
            continue
        }
        log.infof("[%d] Loading %s", i, fi[i].name)
        observations[i] = image
    }

    return observations, prediction
}

write_file :: proc(filepath, data: string) {
    data_as_bytes := transmute([]byte)(data) // 'transmute' casts our string to a byte array
    ok := os.write_entire_file(filepath, data_as_bytes)
    if !ok {
        fmt.println("Error writing file")
    }
}

main :: proc()
{
    logger := log.create_console_logger()
	context.logger = logger

    rl.SetTraceLogLevel(rl.TraceLogLevel.WARNING)
    rl.InitWindow(800, 600, "RMSE Calculator")
    defer rl.CloseWindow()

    region_id := 0

    log.infof("%v", os.args)
    if len(os.args) < 2
    {
        log.error("Usage: rmse path/to/folder <region>")
        return
    }
    else if len(os.args) == 3
    {
        region_id = strconv.atoi(os.args[2])
        log.infof("Region ID: %d", region_id)
    }

    directory: string = os.args[1]
    if !os.is_dir(directory)
    {
        log.errorf("Directory %s does not exist", directory)
        return
    }

    fi, err := read_files(directory)
    if err != nil
    {
        log.errorf("Error reading directory: %v", err)
        return
    }

    // All images are in R8 format (greyscale)
    observations, prediction := load_data(fi)
    
    if prediction.data == nil
    {
        log.error("No prediction image found")
        return
    }
    
    width := int(prediction.width)
    height := int(prediction.height)
    
    // Raylib assume a color is 4 components, R8G8B8A8 (32bit)
    // We only need to consider the R channel
    prediction_pixels := rl.LoadImageColors(prediction)
    
    // result output
    result := strings.builder_make()
    strings.write_string(&result, "Frame,RMSE\n")
    
    for i := 0; i < len(observations); i += 1
    {
        observation_pixels := rl.LoadImageColors(observations[i])
        rmse: f32 = calculate_rmse(prediction_pixels, observation_pixels, width, height, region_id)
        log.infof("RMSE for %d: %.5f", i, rmse)
        
        fmt.sbprintf(&result, "%d,%.5f\n", i, rmse)
    }
    
    filepath := strings.builder_make()

    // takes exiting image name but removes "_00.png"
    filename, ok := strings.substring(fi[0].name, 0, len(fi[0].name) - 7)

    // append region id if specified
    if region_id != 0 { filename = strings.concatenate({filename, "_region_", os.args[2]}) }

    filename = strings.concatenate({filename, ".txt"})
    strings.write_string(&filepath, directory)
    strings.write_string(&filepath, "\\")
    strings.write_string(&filepath, filename)
    
    write_file(fmt.sbprint(&filepath), strings.to_string(result))

    strings.builder_destroy(&result)
    strings.builder_destroy(&filepath)
    log.destroy_console_logger(logger)
}