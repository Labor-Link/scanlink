/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package grandroid.dialog;

import android.app.AlertDialog;
import android.app.Dialog;
import android.content.Context;
import android.content.DialogInterface;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import grandroid.view.LayoutMaker;

/**
 *
 * @author Rovers
 */
public abstract class DialogMask {

    /**
     *
     */
    protected Dialog dialog;
    /**
     *
     */
    protected Context context;
    /**
     *
     */
    protected boolean cancelable;

    /**
     *
     * @param context
     */
    public DialogMask(Context context) {
        this.context = context;
    }

    /**
     *
     * @param context
     * @param cancelable
     */
    public DialogMask(Context context, boolean cancelable) {
        this.context = context;
        this.cancelable = cancelable;
    }

    /**
     *
     * @param context
     * @param maker
     * @return
     * @throws Exception
     */
    public abstract boolean setupDialogContent(Context context, LayoutMaker maker) throws Exception;

    public String getTitle() {
        return null;
    }

    public String getPositiveButtonName() {
        return "OK";
    }

    public String getNegativeButtonName() {
        return "Cancel";
    }

    public boolean onClickPositiveButton(Context context) {
        return true;
    }

    public boolean onClickNegativeButton(Context context) {
        return true;
    }

    /**
     *
     * @param dialogInterface
     */
    public void onDismiss(DialogInterface dialogInterface) {
    }

    /**
     *
     * @param dialogInterface
     */
    public void onCancel(DialogInterface dialogInterface) {
    }

    /**
     *
     * @return
     */
    public Dialog getDialog() {
        return dialog;
    }

    /**
     *
     * @return
     */
    public boolean isShowing() {
        return dialog.isShowing();
    }

    public DialogMask cancelable(boolean bool) {
        this.cancelable = bool;
        return this;
    }

    public DialogMask cancelable() {
        return cancelable(true);
    }

    /**
     *
     */
    public void show() {
        show(DialogStyle.Android);
    }

    /**
     *
     * @param style
     */
    public void show(DialogStyle style) {
        try {
            LinearLayout ll = new LinearLayout(context);
            ll.setOrientation(LinearLayout.VERTICAL);
            LayoutMaker maker = new LayoutMaker(context, ll, false);
            if (style == DialogStyle.Android) {
                AlertDialog.Builder b = new AlertDialog.Builder(context);
                setupDialogContent(context, maker);
                b.setTitle(getTitle());
                b.setView(maker.getMainLayout());
                b.setCancelable(cancelable);
                if (getPositiveButtonName() != null) {
                    b.setPositiveButton(DialogMask.this.getPositiveButtonName(), new DialogInterface.OnClickListener() {
                        public void onClick(DialogInterface dia, int arg1) {
                            if (DialogMask.this.onClickPositiveButton(context)) {
                                dia.dismiss();
                            }
                        }
                    });
                }
                if (getNegativeButtonName() != null) {
                    b.setNegativeButton(DialogMask.this.getNegativeButtonName(), new DialogInterface.OnClickListener() {
                        public void onClick(DialogInterface dia, int arg1) {
                            if (DialogMask.this.onClickNegativeButton(context)) {
                                dia.cancel();
                            }
                        }
                    });
                }
                dialog = b.create();
                dialog.setOnDismissListener(new DialogInterface.OnDismissListener() {
                    public void onDismiss(DialogInterface dialogInterface) {
                        DialogMask.this.onDismiss(dialogInterface);
                    }
                });
                dialog.setOnCancelListener(new DialogInterface.OnCancelListener() {
                    public void onCancel(DialogInterface dialogInterface) {
                        DialogMask.this.onCancel(dialogInterface);
                    }
                });
            } else {

                setupDialogContent(context, maker);
                dialog = create(maker, style);
                dialog.setTitle(getTitle());
                dialog.setContentView(maker.getMainLayout());
                dialog.setOnDismissListener(new DialogInterface.OnDismissListener() {
                    public void onDismiss(DialogInterface dialogInterface) {
                        DialogMask.this.onDismiss(dialogInterface);
                    }
                });
                dialog.setOnCancelListener(new DialogInterface.OnCancelListener() {
                    public void onCancel(DialogInterface dialogInterface) {
                        DialogMask.this.onCancel(dialogInterface);
                    }
                });
            }
            dialog.show();
        } catch (Exception ex) {
            Log.e("grandroid", null, ex);
        }
    }

    public Dialog create(LayoutMaker maker, DialogStyle style) {
        //LayoutInflater inflater = (LayoutInflater) context.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
        // instantiate the dialog with the custom Theme
        if (style != DialogStyle.Android) {
            dialog = new Dialog(context);
        } else {
            dialog = new Dialog(context);
            dialog.setTitle(getTitle());
        }
        dialog.setCancelable(cancelable);
        if (style == DialogStyle.Custom) {
            maker.escape();

            LinearLayout layoutBottom = maker.addRowLayout();
            if (getPositiveButtonName() != null) {
                Button btn = maker.add(maker.createButton(getPositiveButtonName()), maker.layFW(1));

                btn.setOnClickListener(new View.OnClickListener() {
                    public void onClick(View v) {
                        DialogMask.this.onClickPositiveButton(context);
                    }
                });
            }
            // set the cancel button
            if (getNegativeButtonName() != null) {
                Button btn = maker.addButton(getNegativeButtonName());
                btn.setOnClickListener(new View.OnClickListener() {
                    public void onClick(View v) {
                        DialogMask.this.onClickNegativeButton(context);
                    }
                });
            }
        }
        dialog.setContentView(maker.getMainLayout(), new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));
        return dialog;
    }

    public enum DialogStyle {

        Android,
        Custom;
    }
}
