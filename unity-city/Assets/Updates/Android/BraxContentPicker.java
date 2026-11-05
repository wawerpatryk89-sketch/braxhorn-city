package com.braxhorn.content;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

/** System file picker: no storage permission, no access token in the APK. */
public final class BraxContentPicker extends Fragment {
    private String receiver, inbox;
    public static void open(final String receiver, final String inbox) {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            if (activity.getFragmentManager().findFragmentByTag("brax-content-picker") != null) return;
            BraxContentPicker picker = new BraxContentPicker();
            Bundle args = new Bundle(); args.putString("receiver", receiver); args.putString("inbox", inbox);
            picker.setArguments(args);
            activity.getFragmentManager().beginTransaction().add(picker, "brax-content-picker").commit();
        });
    }
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        receiver = getArguments().getString("receiver"); inbox = getArguments().getString("inbox");
        if (state == null) {
            Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE); intent.setType("*/*");
            try { startActivityForResult(intent, 1042); }
            catch (Exception e) { finish("ERROR:Nie można otworzyć wyboru pliku."); }
        }
    }
    @Override public void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != 1042) return;
        if (result != Activity.RESULT_OK || data == null || data.getData() == null) { finish(""); return; }
        final Activity activity = getActivity();
        final android.net.Uri uri = data.getData();
        new Thread(() -> {
            File partial = new File(inbox, "import-" + java.util.UUID.randomUUID() + ".partial");
            try {
                new File(inbox).mkdirs();
                try (InputStream input = activity.getContentResolver().openInputStream(uri);
                     FileOutputStream output = new FileOutputStream(partial)) {
                    if (input == null) throw new java.io.IOException("Brak pliku");
                    byte[] buffer = new byte[131072]; int n; long bytes = 0;
                    while ((n = input.read(buffer)) != -1) {
                        bytes += n;
                        if (bytes > 8L * 1024 * 1024 * 1024) throw new java.io.IOException("Plik przekracza 8 GiB");
                        output.write(buffer, 0, n);
                    }
                    output.getFD().sync();
                }
                File ready = new File(partial.getPath().replace(".partial", ".zip"));
                if (!partial.renameTo(ready)) throw new java.io.IOException("Nie zapisano pliku");
                activity.runOnUiThread(() -> finish(ready.getAbsolutePath()));
            } catch (Exception e) {
                partial.delete();
                activity.runOnUiThread(() -> finish("ERROR:Nie udało się skopiować aktualizacji. Sprawdź plik i wolne miejsce."));
            }
        }, "BraxContentImport").start();
    }
    private void finish(String message) {
        UnityPlayer.UnitySendMessage(receiver, "OnUpdatePicked", message);
        if (isAdded()) getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
    }
}
