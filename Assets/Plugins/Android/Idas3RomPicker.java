package com.haseosora.idas3;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;

import com.unity3d.player.UnityPlayer;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public final class Idas3RomPicker extends Fragment {
    private static final String TAG = "Idas3RomPicker";
    private static final int REQUEST = 0x1D33;
    private String unityObject;
    private String destination;

    public static void open(final String unityObject, final String destination) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            UnityPlayer.UnitySendMessage(unityObject, "OnAndroidRomImportResult", "ERROR:Android activity is unavailable.");
            return;
        }
        activity.runOnUiThread(() -> {
            Idas3RomPicker picker = (Idas3RomPicker) activity.getFragmentManager().findFragmentByTag(TAG);
            if (picker == null) {
                picker = new Idas3RomPicker();
                activity.getFragmentManager().beginTransaction().add(picker, TAG).commitAllowingStateLoss();
                activity.getFragmentManager().executePendingTransactions();
            }
            picker.unityObject = unityObject;
            picker.destination = destination;
            picker.launch();
        });
    }

    private void launch() {
        try {
            Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.setType("*/*");
            startActivityForResult(intent, REQUEST);
        } catch (Exception error) {
            send("ERROR:" + safe(error));
        }
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQUEST) return;
        if (resultCode != Activity.RESULT_OK || data == null || data.getData() == null) {
            send("CANCEL");
            return;
        }
        final Uri uri = data.getData();
        new Thread(() -> copy(uri), "IDAS3-ROM-import").start();
    }

    private void copy(Uri uri) {
        File temporary = null;
        try {
            Activity activity = getActivity();
            if (activity == null) throw new IllegalStateException("Android activity is unavailable.");
            File target = new File(destination).getCanonicalFile();
            File internal = activity.getFilesDir().getCanonicalFile();
            File external = activity.getExternalFilesDir(null);
            if (external != null) external = external.getCanonicalFile();
            String targetPath = target.getPath();
            String internalPrefix = internal.getPath() + File.separator;
            String externalPrefix = external == null ? "" : external.getPath() + File.separator;
            if (!targetPath.startsWith(internalPrefix) &&
                (external == null || !targetPath.startsWith(externalPrefix)))
                throw new SecurityException("ROM destination is outside app-private storage.");
            File parent = target.getParentFile();
            if (parent == null || (!parent.exists() && !parent.mkdirs()))
                throw new IllegalStateException("Could not create the ROM folder.");

            temporary = new File(parent, target.getName() + ".import-" + System.nanoTime());
            try (InputStream input = activity.getContentResolver().openInputStream(uri);
                 FileOutputStream output = new FileOutputStream(temporary)) {
                if (input == null) throw new IllegalStateException("Selected document could not be opened.");
                byte[] buffer = new byte[1024 * 1024];
                int count;
                while ((count = input.read(buffer)) >= 0) {
                    if (count > 0) output.write(buffer, 0, count);
                }
                output.getFD().sync();
            }
            if (temporary.length() <= 0) throw new IllegalStateException("Selected document was empty.");

            File backup = new File(parent, target.getName() + ".previous");
            if (backup.exists() && !backup.delete()) throw new IllegalStateException("Could not clear an old ROM backup.");
            if (target.exists() && !target.renameTo(backup)) throw new IllegalStateException("Could not preserve the existing ROM.");
            if (!temporary.renameTo(target)) {
                if (!target.exists() && backup.exists()) backup.renameTo(target);
                throw new IllegalStateException("Could not finish importing the ROM.");
            }
            if (backup.exists()) backup.delete();
            send("OK");
        } catch (Exception error) {
            if (temporary != null && temporary.exists()) temporary.delete();
            send("ERROR:" + safe(error));
        }
    }

    private void send(String value) {
        if (unityObject != null) UnityPlayer.UnitySendMessage(unityObject, "OnAndroidRomImportResult", value);
    }

    private static String safe(Exception error) {
        String message = error.getMessage();
        return message == null || message.length() == 0 ? error.getClass().getSimpleName() : message.replace('\n', ' ').replace('\r', ' ');
    }
}
