using System;
using System.Drawing;
using System.Windows.Forms;

namespace GrannyManager.App.Pages;

internal static class ListPageGridHelper
{
    private static readonly Color InactiveBack = Color.FromArgb(46, 51, 58);
    private static readonly Color InactiveAltBack = Color.FromArgb(40, 45, 52);
    private static readonly Color InactiveText = Color.FromArgb(145, 154, 166);
    private static readonly Color InactiveSelectionBack = Color.FromArgb(70, 76, 86);
    private static readonly Color ActiveBack = Color.FromArgb(16, 34, 55);
    private static readonly Color ActiveAltBack = Color.FromArgb(13, 29, 48);
    private static readonly Color ActiveText = Color.FromArgb(245, 248, 252);
    private static readonly Color ActiveSelectionBack = Color.FromArgb(52, 82, 120);

    public static void AttachRightClickRemove(DataGridView grid, Action updateSelection, Action removeAction, string removeText = "Remove")
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(removeText, null, (_, _) =>
        {
            updateSelection();
            removeAction();
        });

        grid.ContextMenuStrip = menu;
        grid.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.RowIndex >= grid.Rows.Count)
                return;

            grid.ClearSelection();
            grid.Rows[hit.RowIndex].Selected = true;
            if (hit.ColumnIndex >= 0)
                grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
            else if (grid.Rows[hit.RowIndex].Cells.Count > 0)
                grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[0];

            updateSelection();
        };
    }

    public static void ApplyInactiveRowStyles(DataGridView grid, Func<object, bool> isActiveRecord)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.DataBoundItem is null)
                continue;

            var active = isActiveRecord(row.DataBoundItem);
            if (active)
            {
                row.DefaultCellStyle.BackColor = row.Index % 2 == 0 ? ActiveBack : ActiveAltBack;
                row.DefaultCellStyle.ForeColor = ActiveText;
                row.DefaultCellStyle.SelectionBackColor = ActiveSelectionBack;
                row.DefaultCellStyle.SelectionForeColor = Color.White;
            }
            else
            {
                row.DefaultCellStyle.BackColor = row.Index % 2 == 0 ? InactiveBack : InactiveAltBack;
                row.DefaultCellStyle.ForeColor = InactiveText;
                row.DefaultCellStyle.SelectionBackColor = InactiveSelectionBack;
                row.DefaultCellStyle.SelectionForeColor = Color.White;
            }
        }
    }
}
