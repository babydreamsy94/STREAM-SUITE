"""Stream Suite Update Center desktop application."""

from __future__ import annotations

import argparse
import json
import os
import platform
import subprocess
import sys
import threading
import tkinter as tk
import webbrowser
from pathlib import Path
from tkinter import BOTH, END, LEFT, RIGHT, X, filedialog, messagebox, simpledialog, ttk

from updater_core import (
    AppSettings,
    ExtractedPackage,
    ReleaseCatalog,
    ReleaseManifest,
    SemanticVersion,
    UpdaterError,
    classify_selection,
    download_package,
    format_bytes,
    load_release_catalog,
    load_settings,
    safe_extract_zip,
    save_settings,
    unique_directory,
)

APP_NAME = "Stream Suite Update Center"
APP_VERSION = "0.1.0"
DEFAULT_MANIFEST_URL = (
    "https://raw.githubusercontent.com/"
    "babydreamsy94/STREAM-SUITE/main/deployment/release-catalog.json"
)

COLORS = {
    "background": "#0d1020",
    "surface": "#171b31",
    "surface_alt": "#202642",
    "purple": "#8d78f6",
    "purple_dark": "#6654c6",
    "cyan": "#3edbf0",
    "green": "#53d69b",
    "amber": "#f1bd63",
    "red": "#ff7b8b",
    "text": "#f7f8ff",
    "muted": "#b3bad5",
    "border": "#31395f",
}


class UpdateCenterApp:
    def __init__(
        self,
        root: tk.Tk,
        manifest_source: str,
        settings_path: str | None = None,
    ) -> None:
        self.root = root
        self.manifest_source = manifest_source
        self.settings_path = settings_path
        self.catalog: ReleaseCatalog | None = None
        self.manifest: ReleaseManifest | None = None
        self.extracted_package: ExtractedPackage | None = None
        self.version_choice_map: dict[str, ReleaseManifest] = {}
        self.initial_version_prompted = False
        self.busy = False

        settings_warning = ""
        try:
            self.settings = load_settings(settings_path)
        except UpdaterError as exc:
            self.settings = AppSettings.defaults()
            settings_warning = str(exc)

        self.root.title(APP_NAME)
        self.root.geometry("960x760")
        self.root.minsize(820, 680)
        self.root.configure(bg=COLORS["background"])
        self.root.option_add("*Font", ("Segoe UI", 10))

        self.installed_var = tk.StringVar(value="Not set")
        self.latest_var = tk.StringVar(value="Checking…")
        self.channel_var = tk.StringVar(value="Stable")
        self.compatibility_var = tk.StringVar(value="Checking release compatibility…")
        self.update_state_var = tk.StringVar(value="Checking for updates…")
        self.status_var = tk.StringVar(value="Connecting to the official Stream Suite release feed…")

        self._configure_styles()
        self._build_menu()
        self._build_interface()
        if settings_warning:
            self.root.after(
                200,
                lambda: messagebox.showwarning("Settings Reset", settings_warning, parent=self.root),
            )
        self.root.after(350, self.check_for_updates)

    def _configure_styles(self) -> None:
        style = ttk.Style(self.root)
        try:
            style.theme_use("clam")
        except tk.TclError:
            pass
        style.configure(
            "Suite.Horizontal.TProgressbar",
            troughcolor=COLORS["surface_alt"],
            background=COLORS["cyan"],
            bordercolor=COLORS["surface_alt"],
            lightcolor=COLORS["cyan"],
            darkcolor=COLORS["cyan"],
        )
        style.configure(
            "Suite.TCombobox",
            fieldbackground=COLORS["surface_alt"],
            background=COLORS["surface_alt"],
            foreground=COLORS["text"],
            arrowcolor=COLORS["text"],
            bordercolor=COLORS["border"],
        )

    def _build_menu(self) -> None:
        menu = tk.Menu(self.root)
        settings_menu = tk.Menu(menu, tearoff=False)
        settings_menu.add_command(label="Set Installed Version…", command=self.set_installed_version)
        settings_menu.add_command(label="Choose Download Folder…", command=self.choose_download_folder)
        settings_menu.add_separator()
        settings_menu.add_command(label="Exit", command=self.root.destroy)
        menu.add_cascade(label="Settings", menu=settings_menu)

        help_menu = tk.Menu(menu, tearoff=False)
        help_menu.add_command(label="View Release Page", command=self.open_release_page)
        help_menu.add_command(label="About", command=self.show_about)
        menu.add_cascade(label="Help", menu=help_menu)
        self.root.configure(menu=menu)

    def _build_interface(self) -> None:
        header = tk.Frame(self.root, bg=COLORS["purple"], padx=28, pady=20)
        header.pack(fill=X)
        tk.Label(
            header,
            text="STREAM SUITE",
            bg=COLORS["purple"],
            fg="#ffffff",
            font=("Segoe UI Semibold", 24),
        ).pack(anchor="w")
        tk.Label(
            header,
            text="UPDATE CENTER",
            bg=COLORS["purple"],
            fg="#e9e5ff",
            font=("Segoe UI Semibold", 12),
        ).pack(anchor="w")
        tk.Label(
            header,
            text="Built By Streamers. Powered by Community.",
            bg=COLORS["purple"],
            fg="#ffffff",
            font=("Segoe UI", 10),
        ).pack(anchor="w", pady=(8, 0))

        content = tk.Frame(self.root, bg=COLORS["background"], padx=24, pady=20)
        content.pack(fill=BOTH, expand=True)

        summary = tk.Frame(content, bg=COLORS["background"])
        summary.pack(fill=X)
        self._summary_card(summary, "INSTALLED", self.installed_var).pack(
            side=LEFT, fill=X, expand=True, padx=(0, 6)
        )
        self._summary_card(summary, "LATEST", self.latest_var).pack(
            side=LEFT, fill=X, expand=True, padx=6
        )
        self._summary_card(summary, "CHANNEL", self.channel_var).pack(
            side=LEFT, fill=X, expand=True, padx=(6, 0)
        )

        state_card = tk.Frame(
            content,
            bg=COLORS["surface"],
            highlightthickness=1,
            highlightbackground=COLORS["border"],
            padx=18,
            pady=14,
        )
        state_card.pack(fill=X, pady=(14, 0))
        self.state_label = tk.Label(
            state_card,
            textvariable=self.update_state_var,
            bg=COLORS["surface"],
            fg=COLORS["cyan"],
            font=("Segoe UI Semibold", 14),
        )
        self.state_label.pack(anchor="w")
        tk.Label(
            state_card,
            textvariable=self.compatibility_var,
            bg=COLORS["surface"],
            fg=COLORS["muted"],
            justify=LEFT,
            wraplength=850,
        ).pack(anchor="w", pady=(5, 0))

        notes_header = tk.Frame(content, bg=COLORS["background"])
        notes_header.pack(fill=X, pady=(16, 6))
        tk.Label(
            notes_header,
            text="WHAT’S IN THIS UPDATE",
            bg=COLORS["background"],
            fg=COLORS["text"],
            font=("Segoe UI Semibold", 11),
        ).pack(side=LEFT)
        self.release_link_button = self._button(
            notes_header,
            "Full release notes",
            self.open_release_page,
            compact=True,
        )
        self.release_link_button.pack(side=RIGHT)
        self.release_link_button.configure(state=tk.DISABLED)
        self.version_combo = ttk.Combobox(
            notes_header,
            style="Suite.TCombobox",
            state="disabled",
            width=23,
        )
        self.version_combo.pack(side=RIGHT, padx=(8, 10))
        self.version_combo.bind("<<ComboboxSelected>>", self._version_selected)
        tk.Label(
            notes_header,
            text="Version library:",
            bg=COLORS["background"],
            fg=COLORS["muted"],
            font=("Segoe UI", 9),
        ).pack(side=RIGHT)

        notes_frame = tk.Frame(
            content,
            bg=COLORS["surface"],
            highlightthickness=1,
            highlightbackground=COLORS["border"],
        )
        notes_frame.pack(fill=BOTH, expand=True)
        self.notes_text = tk.Text(
            notes_frame,
            height=9,
            wrap="word",
            bg=COLORS["surface"],
            fg=COLORS["text"],
            insertbackground=COLORS["text"],
            selectbackground=COLORS["purple_dark"],
            relief="flat",
            padx=16,
            pady=13,
            spacing1=3,
            spacing3=5,
        )
        notes_scroll = ttk.Scrollbar(notes_frame, command=self.notes_text.yview)
        self.notes_text.configure(yscrollcommand=notes_scroll.set)
        notes_scroll.pack(side=RIGHT, fill="y")
        self.notes_text.pack(side=LEFT, fill=BOTH, expand=True)
        self._set_notes("Checking the official release information…")

        action_row = tk.Frame(content, bg=COLORS["background"])
        action_row.pack(fill=X, pady=(14, 8))
        self.check_button = self._button(action_row, "Check Again", self.check_for_updates)
        self.check_button.pack(side=LEFT)
        self.download_button = self._button(
            action_row, "Download Update", self.download_update, primary=True
        )
        self.download_button.pack(side=LEFT, padx=(10, 0))
        self.download_button.configure(state=tk.DISABLED)
        self.folder_button = self._button(action_row, "Open Update Folder", self.open_update_folder)
        self.folder_button.pack(side=LEFT, padx=(10, 0))
        self.folder_button.configure(state=tk.DISABLED)
        self.guide_button = self._button(action_row, "Open Setup Guide", self.open_guide)
        self.guide_button.pack(side=LEFT, padx=(10, 0))
        self.guide_button.configure(state=tk.DISABLED)
        self.mark_installed_button = self._button(
            action_row, "Mark Installed", self.mark_latest_installed
        )
        self.mark_installed_button.pack(side=RIGHT)
        self.mark_installed_button.configure(state=tk.DISABLED)

        self.progress = ttk.Progressbar(
            content,
            style="Suite.Horizontal.TProgressbar",
            mode="determinate",
            maximum=100,
        )
        self.progress.pack(fill=X, pady=(2, 7))
        tk.Label(
            content,
            textvariable=self.status_var,
            bg=COLORS["background"],
            fg=COLORS["muted"],
            justify=LEFT,
            wraplength=880,
        ).pack(anchor="w")

        safety = tk.Frame(
            content,
            bg="#242033",
            highlightthickness=1,
            highlightbackground="#5c4f74",
            padx=14,
            pady=10,
        )
        safety.pack(fill=X, pady=(14, 0))
        tk.Label(
            safety,
            text="SAFE UPDATE DESIGN",
            bg="#242033",
            fg=COLORS["amber"],
            font=("Segoe UI Semibold", 9),
        ).pack(anchor="w")
        tk.Label(
            safety,
            text=(
                "This tool downloads and verifies official packages. It never edits Streamer.bot "
                "or imports actions automatically. You remain in control of backups, overwrite "
                "choices, and personal configuration."
            ),
            bg="#242033",
            fg=COLORS["text"],
            justify=LEFT,
            wraplength=850,
        ).pack(anchor="w", pady=(3, 0))

    def _summary_card(self, parent: tk.Widget, heading: str, variable: tk.StringVar) -> tk.Frame:
        card = tk.Frame(
            parent,
            bg=COLORS["surface"],
            highlightthickness=1,
            highlightbackground=COLORS["border"],
            padx=16,
            pady=12,
        )
        tk.Label(
            card,
            text=heading,
            bg=COLORS["surface"],
            fg=COLORS["muted"],
            font=("Segoe UI Semibold", 8),
        ).pack(anchor="w")
        tk.Label(
            card,
            textvariable=variable,
            bg=COLORS["surface"],
            fg=COLORS["text"],
            font=("Segoe UI Semibold", 15),
        ).pack(anchor="w", pady=(2, 0))
        return card

    def _button(
        self,
        parent: tk.Widget,
        text: str,
        command: object,
        primary: bool = False,
        compact: bool = False,
    ) -> tk.Button:
        background = COLORS["purple"] if primary else COLORS["surface_alt"]
        active = COLORS["purple_dark"] if primary else COLORS["border"]
        return tk.Button(
            parent,
            text=text,
            command=command,
            bg=background,
            fg="#ffffff",
            activebackground=active,
            activeforeground="#ffffff",
            disabledforeground="#78809e",
            relief="flat",
            cursor="hand2",
            padx=10 if compact else 16,
            pady=4 if compact else 8,
            font=("Segoe UI Semibold", 9),
        )

    def _set_notes(self, text: str) -> None:
        self.notes_text.configure(state=tk.NORMAL)
        self.notes_text.delete("1.0", END)
        self.notes_text.insert("1.0", text)
        self.notes_text.configure(state=tk.DISABLED)

    def _set_busy(self, busy: bool, status: str | None = None) -> None:
        self.busy = busy
        self.check_button.configure(state=tk.DISABLED if busy else tk.NORMAL)
        if busy:
            self.download_button.configure(state=tk.DISABLED)
        elif self.catalog is not None and self.manifest is not None:
            self.download_button.configure(state=tk.NORMAL)
        if status:
            self.status_var.set(status)

    def _background(
        self,
        worker: object,
        success: object,
        working_status: str,
    ) -> None:
        if self.busy:
            return
        self._set_busy(True, working_status)

        def execute() -> None:
            try:
                result = worker()
            except Exception as exc:  # noqa: BLE001 - final GUI exception boundary
                self.root.after(0, lambda captured=exc: self._task_failed(captured))
            else:
                self.root.after(0, lambda: self._task_succeeded(success, result))

        threading.Thread(target=execute, daemon=True).start()

    def _task_succeeded(self, success: object, result: object) -> None:
        self._set_busy(False)
        success(result)

    def _task_failed(self, error: Exception) -> None:
        self._set_busy(False, "The requested operation did not complete.")
        self.progress.configure(value=0)
        if isinstance(error, UpdaterError):
            detail = str(error)
        else:
            detail = f"Unexpected error: {error}"
        messagebox.showerror("Stream Suite Update Error", detail, parent=self.root)

    def check_for_updates(self) -> None:
        self.progress.configure(value=0)
        self.latest_var.set("Checking…")
        self.update_state_var.set("Checking for updates…")
        self.state_label.configure(fg=COLORS["cyan"])
        self._background(
            lambda: load_release_catalog(self.manifest_source),
            self._display_catalog,
            "Checking the official Stream Suite release feed…",
        )

    def _display_catalog(self, result: object) -> None:
        if not isinstance(result, ReleaseCatalog):
            raise TypeError("Expected release catalog result.")
        self.catalog = result
        self.version_choice_map = {}
        choices: list[str] = []
        latest_release = result.latest()
        latest_label = ""
        for release in result.releases:
            suffix = " — Latest" if release == latest_release else ""
            label = f"{release.display_version} ({release.channel.title()}){suffix}"
            choices.append(label)
            self.version_choice_map[label] = release
            if release == latest_release:
                latest_label = label
        self.version_combo.configure(values=choices, state="readonly")
        self.version_combo.set(latest_label)
        self._display_manifest(latest_release)
        if len(result.releases) == 1:
            self.status_var.set(
                "Release information checked successfully. Previous versions will appear "
                "here as they are added to the catalog."
            )
        if self.settings.installed_version is None and not self.initial_version_prompted:
            self.initial_version_prompted = True
            self.root.after(250, self._offer_initial_version_setup)

    def _offer_initial_version_setup(self) -> None:
        value = simpledialog.askstring(
            "Welcome to Stream Suite Update Center",
            "Which Stream Suite version is currently installed?\n\n"
            "Enter a version such as 4.0.0, or select Cancel if this is your first installation.",
            initialvalue="4.0.0",
            parent=self.root,
        )
        if value is None:
            return
        try:
            self.settings.installed_version = str(SemanticVersion.parse(value))
        except ValueError as exc:
            messagebox.showerror("Invalid Version", str(exc), parent=self.root)
            return
        self._save_settings()
        if self.manifest:
            self._display_manifest(self.manifest)

    def _version_selected(self, _event: object = None) -> None:
        selected = self.version_choice_map.get(self.version_combo.get())
        if selected:
            self.extracted_package = None
            self.folder_button.configure(state=tk.DISABLED)
            self.guide_button.configure(state=tk.DISABLED)
            self.mark_installed_button.configure(state=tk.DISABLED)
            self.progress.configure(value=0)
            self._display_manifest(selected)

    def _display_manifest(self, manifest: ReleaseManifest) -> None:
        self.manifest = manifest
        latest = self.catalog.latest() if self.catalog else manifest
        self.installed_var.set(self.settings.installed_version or "Not set")
        self.latest_var.set(latest.display_version)
        self.channel_var.set(manifest.channel.title())
        selection_type = classify_selection(
            self.settings.installed_version, manifest.suite_version
        )

        if selection_type == "package":
            self.update_state_var.set(f"Selected package: {manifest.display_version}")
            self.state_label.configure(fg=COLORS["amber"])
            self.download_button.configure(text="Download Package")
        elif selection_type == "downgrade":
            self.update_state_var.set(f"Previous version selected: {manifest.display_version}")
            self.state_label.configure(fg=COLORS["amber"])
            self.download_button.configure(text="Download Downgrade")
        elif selection_type == "update":
            self.update_state_var.set(f"Update available: {manifest.display_version}")
            self.state_label.configure(fg=COLORS["cyan"])
            self.download_button.configure(text="Download Update")
        elif selection_type == "reinstall":
            if manifest == latest:
                self.update_state_var.set("You’re up to date!")
            else:
                self.update_state_var.set(f"Installed version selected: {manifest.display_version}")
            self.state_label.configure(fg=COLORS["green"])
            self.download_button.configure(text="Download Again")
        environment = self._detected_environment()
        support = self._platform_support(environment)
        support_text = (
            f"Designed for Streamer.bot {manifest.streamer_bot_minimum}+; "
            f"tested on {manifest.streamer_bot_tested}. Detected: {environment}."
        )
        if support:
            support_text += f" Status: {support.status.replace('-', ' ').title()}."
            if support.note:
                support_text += f" {support.note}"
        if manifest.breaking_changes:
            support_text += " This release contains setup changes—read the guide before importing."
        self.compatibility_var.set(support_text)

        note_lines = [f"{manifest.release_name} — {manifest.release_date}", ""]
        if manifest.notice:
            note_lines.extend([manifest.notice, ""])
        if manifest.release_notes:
            note_lines.extend(f"• {note}" for note in manifest.release_notes)
        else:
            note_lines.append("No release notes were supplied.")
        note_lines.extend(
            [
                "",
                f"Package: {manifest.package.file_name}",
                f"Download size: {format_bytes(manifest.package.size_bytes)}",
                "Installation: Guided Streamer.bot import (never automatic)",
            ]
        )
        self._set_notes("\n".join(note_lines))
        self.release_link_button.configure(
            state=tk.NORMAL if manifest.release_notes_url else tk.DISABLED
        )
        self.download_button.configure(state=tk.NORMAL)
        if self.catalog and len(self.catalog.releases) > 1:
            self.status_var.set(
                f"Version Library ready: {len(self.catalog.releases)} releases available."
            )
        else:
            self.status_var.set("Release information checked successfully.")

    def _detected_environment(self) -> str:
        if os.environ.get("WINEPREFIX") or os.environ.get("WINELOADERNOEXEC"):
            return "Linux/Wine"
        if os.name == "nt":
            return "Windows"
        if sys.platform.startswith("linux"):
            return "Linux/Wine"
        return platform.system() or "Unknown"

    def _platform_support(self, environment: str):
        if not self.manifest:
            return None
        wanted = "linux" if "linux" in environment.lower() else environment.lower()
        for item in self.manifest.platforms:
            if wanted in item.name.lower() or item.name.lower() in wanted:
                return item
        return None

    def download_update(self) -> None:
        if not self.manifest:
            self.check_for_updates()
            return

        manifest = self.manifest
        is_downgrade = (
            classify_selection(self.settings.installed_version, manifest.suite_version)
            == "downgrade"
        )
        prompt = (
            f"Download and verify {manifest.release_name}?\n\n"
            f"The files will be placed in:\n{self.settings.download_directory}\n\n"
            "This does not modify Streamer.bot. You will complete the import yourself."
        )
        dialog_title = "Download Stream Suite Update"
        if is_downgrade:
            dialog_title = "Download Previous Stream Suite Version"
            prompt += (
                "\n\nIMPORTANT DOWNGRADE NOTICE:\nAn older action package may not understand "
                "data written by a newer version. If you are recovering from a failed update, "
                "restoring the Streamer.bot backup created before that update is usually safer "
                "than importing older actions over newer ones."
            )
        if not messagebox.askyesno(dialog_title, prompt, parent=self.root):
            return

        self.progress.configure(value=0)

        def progress_download(received: int, total: int | None) -> None:
            if total and total > 0:
                value = min(70, int(received / total * 70))
            else:
                value = min(65, int(received / (2 * 1024 * 1024) * 65))
            self.root.after(
                0,
                lambda: (
                    self.progress.configure(value=value),
                    self.status_var.set(
                        f"Downloading verified package… {format_bytes(received)}"
                    ),
                ),
            )

        def progress_extract(current: int, total: int) -> None:
            value = 70 + int(current / max(total, 1) * 30)
            self.root.after(
                0,
                lambda: (
                    self.progress.configure(value=value),
                    self.status_var.set(f"Safely extracting package… {current} of {total}"),
                ),
            )

        def worker() -> ExtractedPackage:
            base = Path(self.settings.download_directory).expanduser()
            package_path = download_package(
                manifest,
                base / "Downloaded Packages",
                progress=progress_download,
            )
            extraction_root = unique_directory(
                base,
                f"Stream Suite {manifest.display_version}",
            )
            return safe_extract_zip(
                package_path,
                extraction_root,
                guide_file=manifest.installation.guide_file,
                progress=progress_extract,
            )

        self._background(worker, self._download_complete, "Preparing the update download…")

    def _download_complete(self, result: object) -> None:
        if not isinstance(result, ExtractedPackage):
            raise TypeError("Expected extracted package result.")
        self.extracted_package = result
        self.progress.configure(value=100)
        self.status_var.set(
            f"Update downloaded and verified. Found {len(result.streamer_bot_imports)} "
            "Streamer.bot import file(s)."
        )
        self.folder_button.configure(state=tk.NORMAL)
        self.guide_button.configure(
            state=tk.NORMAL if result.guide_file else tk.DISABLED
        )
        self.mark_installed_button.configure(state=tk.NORMAL)
        message = (
            "The update was downloaded, verified, and extracted successfully.\n\n"
            "Nothing in Streamer.bot has been changed. Before importing, confirm that you "
            "have a recent backup and read the included setup guide."
        )
        messagebox.showinfo("Update Ready", message, parent=self.root)

    def set_installed_version(self) -> None:
        initial = self.settings.installed_version or (
            self.manifest.suite_version if self.manifest else "4.0.0"
        )
        value = simpledialog.askstring(
            "Installed Stream Suite Version",
            "Which Stream Suite version is currently installed?\nExample: 4.0.0",
            initialvalue=initial,
            parent=self.root,
        )
        if value is None:
            return
        try:
            normalized = str(SemanticVersion.parse(value))
        except ValueError as exc:
            messagebox.showerror("Invalid Version", str(exc), parent=self.root)
            return
        self.settings.installed_version = normalized
        self._save_settings()
        if self.manifest:
            self._display_manifest(self.manifest)

    def mark_latest_installed(self) -> None:
        if not self.manifest:
            return
        prompt = (
            f"Have you finished importing and configuring {self.manifest.display_version} "
            "inside Streamer.bot?\n\n"
            "Downloading the files alone does not mean the update is installed."
        )
        if not messagebox.askyesno("Confirm Installation", prompt, parent=self.root):
            return
        self.settings.installed_version = self.manifest.suite_version
        self._save_settings()
        self._display_manifest(self.manifest)
        self.status_var.set(f"Marked Stream Suite {self.manifest.display_version} as installed.")

    def choose_download_folder(self) -> None:
        selected = filedialog.askdirectory(
            title="Choose Stream Suite Update Folder",
            initialdir=self.settings.download_directory,
            mustexist=False,
            parent=self.root,
        )
        if not selected:
            return
        self.settings.download_directory = selected
        self._save_settings()
        self.status_var.set(f"Future updates will download to: {selected}")

    def _save_settings(self) -> None:
        try:
            save_settings(self.settings, self.settings_path)
        except UpdaterError as exc:
            messagebox.showerror("Could Not Save Settings", str(exc), parent=self.root)

    def open_update_folder(self) -> None:
        if self.extracted_package:
            self._open_path(self.extracted_package.root)

    def open_guide(self) -> None:
        if self.extracted_package and self.extracted_package.guide_file:
            self._open_path(self.extracted_package.guide_file)

    def open_release_page(self) -> None:
        if self.manifest and self.manifest.release_notes_url:
            webbrowser.open(self.manifest.release_notes_url)

    def _open_path(self, path: Path) -> None:
        try:
            if os.name == "nt":
                os.startfile(str(path))  # type: ignore[attr-defined]
            elif sys.platform == "darwin":
                subprocess.Popen(["open", str(path)])
            else:
                subprocess.Popen(["xdg-open", str(path)])
        except OSError as exc:
            messagebox.showerror("Could Not Open File", str(exc), parent=self.root)

    def show_about(self) -> None:
        messagebox.showinfo(
            "About Stream Suite Update Center",
            f"{APP_NAME}\nVersion {APP_VERSION}\n\n"
            "Created for Stream Suite by babydreamsy.\n"
            "Built By Streamers. Powered by Community.\n\n"
            "The Update Center downloads and verifies official packages but never "
            "changes Streamer.bot automatically.",
            parent=self.root,
        )


def diagnostics(manifest_source: str) -> int:
    try:
        catalog = load_release_catalog(manifest_source)
        manifest = catalog.latest()
    except UpdaterError as exc:
        print(json.dumps({"ok": False, "error": str(exc)}, indent=2))
        return 1
    print(
        json.dumps(
            {
                "ok": True,
                "updaterVersion": APP_VERSION,
                "suiteVersion": manifest.suite_version,
                "displayVersion": manifest.display_version,
                "channel": manifest.channel,
                "package": manifest.package.file_name,
                "sizeBytes": manifest.package.size_bytes,
                "streamerBotTested": manifest.streamer_bot_tested,
                "platforms": [
                    {"name": item.name, "status": item.status}
                    for item in manifest.platforms
                ],
                "availableVersions": [
                    item.suite_version for item in catalog.releases
                ],
            },
            indent=2,
        )
    )
    return 0


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=APP_NAME)
    parser.add_argument(
        "--manifest",
        default=DEFAULT_MANIFEST_URL,
        help="HTTPS URL or local path to an update manifest.",
    )
    parser.add_argument(
        "--settings",
        default=None,
        help="Optional settings path for portable/testing use.",
    )
    parser.add_argument(
        "--diagnostics",
        action="store_true",
        help="Validate the manifest and print a machine-readable summary without opening the GUI.",
    )
    parser.add_argument("--version", action="version", version=f"%(prog)s {APP_VERSION}")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    if args.diagnostics:
        return diagnostics(args.manifest)
    root = tk.Tk()
    UpdateCenterApp(root, args.manifest, args.settings)
    root.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
