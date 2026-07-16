# Live_CPR_Feedback_App

A **C#/.NET MAUI** app that provides live feedback during a hypothetical CPR.

> ⚠️ **Disclaimer:** This project is intended for **educational purposes only** and must **not** be used in real emergency situations.

---

## Functional Description

The app calculates compression metrics using one of two interchangeable mechanisms:

| Mechanism | Status |
| --- | --- |
| Signal processing | Pipeline fully implemented, currently defective |
| AI (ONNX model) | Working |

The folders with the `AAA_` prefix contain the groundwork performed for this project:

- AI training with MATLAB
- Signal processing in MATLAB
- Original feedback algorithm
- MAUI AI prototype
- Small MAUI data collection app

### Architecture

The app follows a **separation of concerns** approach and uses the MAUI **Model-View-ViewModel (MVVM)** architecture.
Key functionality can be found in the `Services` folder.

### Pages

- **Main Page** → **Prepare Smartphone Page** → **Instructions: Perform CPR Page** → **CPR Page**
- About
- CPR Infos
- CPR Infos Video
- Device Sensor Page
- Metronome Page
- Settings Page
- Storage Page

---

## How It Works

1. The app reads data from the smartphone's **accelerometer sensor**.
2. It calculates **chest compression depth** and **compressions per minute**, using either signal processing or a trained AI model.
3. Based on that output, it provides **visual and auditory feedback**.

---

## Background

This app was created as a project at **Pforzheim University** in the bachelor's program of **Medical Engineering**.

**Developed by:** Sanjita Basnet, Michael Kiryokoz, Jan Klittich and Nico Reim
**Period:** mainly during the summer semester 2026 (approx. 2026-03-16 to 2026-07-15)

---

## Side Note

[Material Symbols](https://fonts.google.com/icons) by Google are used for the menu icons of *Settings* and *Storage*.
