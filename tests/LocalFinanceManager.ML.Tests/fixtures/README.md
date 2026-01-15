# ML Test Fixtures

This directory contains pre-trained ML.NET model files (.bin) used for testing.

## Structure

- `models/` - Pre-trained .bin model files

## Notes

- Fixture models will be populated during MVP-5 (Learning Categorization)
- Models are committed to the repository to avoid re-training during CI
- Models should be small (<1MB) and trained on minimal datasets for speed
