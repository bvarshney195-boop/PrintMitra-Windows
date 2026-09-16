# Release checklist

## Automated

- [ ] Windows GitHub Actions restore passes
- [ ] Release build passes without warnings promoted to errors
- [ ] Self-contained win-x64 publish succeeds
- [ ] Published artifact starts on a clean Windows 10/11 test machine

## Printer acceptance

- [ ] Canon G2000 appears in installed-printer dropdown
- [ ] Native Canon Printer Properties opens
- [ ] A4 paper and Actual Size / 100% are selected
- [ ] Calibration sheet 100 mm line and 50 mm box measured
- [ ] Aadhaar/PAN 85.60 × 53.98 mm output is within ±0.5 mm after calibration
- [ ] Front/back layout remains inside printable area
- [ ] Cancelled print leaves no retained sensitive image

## Image acceptance

- [ ] Straight card photograph corrects accurately
- [ ] Angled photograph corrects accurately
- [ ] Dark/light backgrounds are detected
- [ ] Low-confidence detection does not invent missing content
- [ ] Original image is unchanged

## Privacy

- [ ] Application works with network disconnected
- [ ] No outbound network attempt during normal use
- [ ] Temporary session directory is deleted on exit
- [ ] Stale-session cleanup runs on next launch
- [ ] Logs contain no image bytes or identity numbers

## Release

- [ ] Version updated
- [ ] Executable and installer code-signed
- [ ] Clean-machine antivirus scan passes
- [ ] User guide included
- [ ] Release notes state verified printer models
