# VeloxOptiSL

<p align="center">
  <b>A high-performance, hardened Windows desktop utility built with C# and WPF, engineered with robust system safety, single-instance enforcement, global exception shielding, and automated release monitoring.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Status-Work%20In%20Progress-orange?style=for-the-badge" alt="Project Status: Work In Progress">
  <img src="https://img.shields.io/badge/Platform-Windows-blue?style=for-the-badge" alt="Platform: Windows">
  <img src="https://img.shields.io/badge/.NET-WPF-purple?style=for-the-badge" alt="Framework: .NET WPF">
</p>

---

> **🚧 Project Status & Stability Notice:** **VeloxOptiSL** is currently under active, ongoing development (Work In Progress). Because the project is continuously evolving, **bugs, errors, or unexpected behavior may occasionally occur**. Feedback and reports help shape future stability as the application matures!

---

## 🖼️ Launcher Preview

*A visual look at the VeloxOptiSL interface in action:*

<p align="center">
  <!-- Replace the link below with your actual screenshot file path once uploaded to your repository, e.g., screenshots/launcher-main.png -->
  <img src="screenshots/launcher-main.png" alt="VeloxOptiSL Launcher Interface" width="800px" style="border-radius: 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.2);">
</p>

*(Note: Additional UI screenshots and feature walkthroughs will be added here as the layout evolves.)*

---

## 📖 Overview

**VeloxOptiSL** is a production-grade, performance-focused Windows Presentation Foundation (WPF) desktop utility engineered with a heavy focus on stability, fault tolerance, and clean architecture. Designed specifically to optimize and elevate the *Second Life* experience across a wide spectrum of hardware, it bridges modern .NET capabilities with lightweight deployment. While primarily built to empower users on low-end and mid-end PCs—spanning both desktops and laptops—with smoother execution and better resource handling, it also delivers valuable stability and performance benefits for high-end systems, ensuring a seamless, protected, and enhanced experience for all *Second Life* users while guarding against common runtime failures and multi-instance resource contention.

---

## ✨ Key Features

* **Hardware-Inclusive Performance Optimization**
  * Engineered to elevate and smooth out the *Second Life* experience for users across all hardware tiers.
  * Primarily designed to empower low-end and mid-end desktops and laptops, while also providing reliable stability and performance enhancements for high-end systems.

* **Smart Single-Instance Enforcement**
  * Utilizes a native system-wide `Mutex` to guard against multi-instance execution.
  * Automatically prevents multiple copies of the launcher from running simultaneously, keeping system resources clean and conflict-free.

* **Automated GitHub Update Checker**
  * Asynchronously pings the official GitHub Releases API on startup to check for newer application versions.
  * Notifies users when an update is available so they always have access to the latest improvements and fixes.

* **Global Exception Shielding**
  * Automatically catches unhandled exceptions across both the UI thread (`DispatcherUnhandledException`) and background/AppDomain threads.
  * Prevents hard crashes and ensures the application handles unexpected runtime faults gracefully.

* **Asynchronous Crash Logging**
  * Writes detailed diagnostic metadata (timestamps, exception types, and full stack traces) directly to a local `crash_report.log` file.
  * Uses non-blocking file I/O operations to record errors safely without freezing the app.

* **Portable & Lightweight Deployment**
  * Compiled as a standalone, single-file executable (`PublishSingleFile`) for maximum portability.
  * Runs directly out-of-the-box with zero complex installation routines or pre-installed runtimes required.

---

## 🛡️ Under the Hood: Core Architecture

VeloxOptiSL incorporates enterprise-level hardening measures directly into its application lifecycle (`App.xaml.cs`):

### 1. Single-Instance Mutex Enforcement
* **Mechanism:** Utilizes a native system-wide `Mutex` (`VeloxOptiSL_SingleInstance_Mutex_Key`) to guard against multi-instance execution.
* **Behavior:** If an instance is already running, the application gracefully alerts the user via a native message box and terminates safely before initializing the UI thread.

### 2. Global Exception Shielding & Asynchronous Logging
* **UI & Background Traps:** Automatically captures unhandled exceptions across both the UI thread (`DispatcherUnhandledException`) and background/AppDomain threads (`AppDomain.CurrentDomain.UnhandledException`).
* **Non-Blocking Logging:** Writes detailed crash metadata asynchronously to a local `crash_report.log` file using non-blocking file I/O operations (`File.AppendAllTextAsync`).

### 3. Automated Release Monitoring
* **API Integration:** Asynchronously pings the official GitHub Releases API (`https://api.github.com/repos/KingVJoker/VeloxOptiSL/releases/latest`) on startup to evaluate version compatibility and notify users of available updates.

---

## 🛠️ Technical Stack & Specifications

* **Language:** C#
* **Framework:** .NET / WPF (Windows Presentation Foundation)
* **Deployment Profile:** Optimized for standalone, single-file packaging (`PublishSingleFile`) for maximum portability.
* **Version Control:** Git & GitHub

---

## 🚀 Getting Started

1. Head over to the [Releases tab](../../releases/latest).
2. Download the latest compiled single-file executable (`VeloxOptiSL.exe`).
3. Run the application directly—no complex installation routines or pre-installed runtimes required.

---

## 🗺️ Development Roadmap & Future Plans

Because this project is actively evolving, upcoming milestones include:
* Expanded configuration options and fine-tuning controls.
* Enhanced UI telemetry and real-time status indicators.
* Future developer documentation and contribution guides as the codebase matures.

---

## 📂 Project Structure

* `App.xaml.cs` — Core lifecycle management, Mutex handling, global exception routing, update orchestration, and clean exit overrides (`OnStartup`, `OnExit`).
* `MainWindow.xaml.cs` — Primary application interface and UI logic.
* `crash_report.log` — Local diagnostic log generated automatically during unexpected runtime faults.

---

## 📝 License & Development Notes

* Developed with **AI assistance** to ensure robust architectural standards, clean threading patterns, and secure exception handling.
* Maintained by **KingVJoker**.
