package com.kittecnico.util;

import com.sun.jna.Library;
import com.sun.jna.Native;
import com.sun.jna.Pointer;
import com.sun.jna.platform.win32.WinDef;
import javafx.stage.Stage;
import javafx.stage.Window;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.lang.reflect.Method;

/**
 * Aplica o efeito de Vidro "Mica" do Windows 11 usando a API DWM
 * via JNA (Java Native Access), sem necessitar de DLLs externas.
 *
 * Funciona apenas no Windows 11 Build 22000+.
 * Em versões mais antigas, o efeito é ignorado silenciosamente.
 */
public class WindowStyleUtil {

    private static final Logger log = LoggerFactory.getLogger(WindowStyleUtil.class);

    private static final int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private static final int DWMWA_SYSTEMBACKDROP_TYPE     = 38;  // Windows 11 22H2+
    private static final int DWMWA_MICA_EFFECT             = 1029; // Fallback Windows 11 21H2

    interface DwmApi extends Library {
        DwmApi INSTANCE = Native.load("dwmapi", DwmApi.class);
        int DwmSetWindowAttribute(Pointer hwnd, int dwAttribute, Pointer pvAttribute, int cbAttribute);
    }

    /**
     * Aplica o backdrop Mica + Dark Mode title bar na janela JavaFX.
     */
    public static void applyMicaBackdrop(Stage stage) {
        try {
            long hwnd = getWindowHandle(stage);
            if (hwnd == 0) {
                log.warn("Não foi possível obter o HWND da janela.");
                return;
            }

            Pointer hWndPtr = new Pointer(hwnd);

            // Força a barra de título escura
            try (com.sun.jna.Memory mem = new com.sun.jna.Memory(4)) {
                mem.setInt(0, 1);
                DwmApi.INSTANCE.DwmSetWindowAttribute(hWndPtr, DWMWA_USE_IMMERSIVE_DARK_MODE, mem, 4);
            }

            // Tenta Mica (Windows 11 22H2+)
            try (com.sun.jna.Memory mem = new com.sun.jna.Memory(4)) {
                mem.setInt(0, 2); // 2 = Mica, 3 = Acrylic, 4 = MicaTabbed
                int result = DwmApi.INSTANCE.DwmSetWindowAttribute(hWndPtr, DWMWA_SYSTEMBACKDROP_TYPE, mem, 4);
                if (result == 0) {
                    log.info("✅ Efeito Mica aplicado com sucesso!");
                    return;
                }
            }

            // Fallback: Mica effect para Windows 11 21H2
            try (com.sun.jna.Memory mem = new com.sun.jna.Memory(4)) {
                mem.setInt(0, 1);
                DwmApi.INSTANCE.DwmSetWindowAttribute(hWndPtr, DWMWA_MICA_EFFECT, mem, 4);
                log.info("✅ Efeito Mica (fallback) aplicado!");
            }

        } catch (Throwable e) {
            // Ignora silenciosamente em sistemas não-Windows ou sem suporte
            log.debug("Mica effect não suportado: {}", e.getMessage());
        }
    }

    /** Obtém o HWND nativo da janela JavaFX via reflexão */
    private static long getWindowHandle(Stage stage) {
        try {
            // JavaFX 21 expõe o peer nativo via TKStage
            Method getPeer = Window.class.getDeclaredMethod("getPeer");
            getPeer.setAccessible(true);
            Object peer = getPeer.invoke(stage);

            if (peer == null) return 0;

            // Tenta obter o raw handle
            Method getRawHandle = peer.getClass().getMethod("getRawHandle");
            getRawHandle.setAccessible(true);
            return (long) getRawHandle.invoke(peer);

        } catch (Exception e) {
            log.debug("Não foi possível obter HWND via reflexão: {}", e.getMessage());
            return 0;
        }
    }
}
