#!/bin/python3

import os
import sys

import main


def ensure_venv():
    # Detect if we are running inside a virtual environment
    # sys.prefix == sys.base_prefix means we are in the global environment
    in_venv = sys.prefix != sys.base_prefix

    if not in_venv:
        print("⚠️ Not in a virtual environment. Attempting to restart...")

        # Define the relative path to the virtual env's python executable
        # Windows uses 'Scripts/python.exe', while macOS/Linux uses 'bin/python'
        venv_dir = "venv"  # Change this if your folder has a different name

        if os.name == "nt":
            venv_python = os.path.join(venv_dir, "Scripts", "python.exe")
        else:
            venv_python = os.path.join(venv_dir, "bin", "python3")

        # Check if the virtual environment actually exists
        if not os.path.exists(venv_python):
            print(f"❌ Error: Virtual environment not found at '{venv_dir}'.")
            print("Please create it first (e.g., 'python3 -m venv venv').")
            print("And install dependencies `pip3 install -r requiremnts.txt`")
            sys.exit(1)

        print(f"🚀 Restarting script using: {venv_python}\n" + "-" * 40)
        os.execv(venv_python, [venv_python] + sys.argv)


def load_succes_msg():
    # ==========================================
    # YOUR ACTUAL SCRIPT LOGIC GOES HERE
    # ==========================================
    print("✅ Success! Running smoothly inside the virtual environment.")
    print(f"Current Python executable: {sys.executable}")


# Run the check immediately
ensure_venv()
load_succes_msg()
main.run()
