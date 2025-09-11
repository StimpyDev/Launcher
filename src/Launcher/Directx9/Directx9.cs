using System.Runtime.InteropServices;

public static partial class D3D9
{
    [LibraryImport("d3d9.dll", EntryPoint = "Direct3DCreate9")]
    public static partial nint Direct3DCreate9(uint sdkVersion);

    public static bool IsAvailable()
    {
        try
        {
            var v = Direct3DCreate9(0x20);
            if (v != 0)
            {
                Marshal.Release(v);
                return true;
            }
        }
        catch
        {

        }
        return false;
    }
}