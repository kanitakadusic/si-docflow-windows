## T1 - Receive Processed Document for Review

**Description:** The application should retrieve the processed document from the server and display it to the user.

- **Preconditions:** The server is active and available.
- **Steps:**
  1. Launch the application.
  2. Enter the name and select the CIPS document type.
  3. Upload the document and click submit.
- **Expected Result:** The document is displayed in the user interface with recognized OCR fields.
- **Negative Test:** Select a document type for which no layer is defined.

---

## T2 - Final Submission

**Description:** The user confirms the document after it has been reviewed and corrected.

- **Preconditions:** The document has been corrected.
- **Steps:**
  1. Launch the application.
  2. Enter the name and select the CIPS document type.
  3. Upload the document and click submit.
  4. Click the finalize button.
- **Expected Result:** The document is saved and can no longer be edited.
- **Negative Test:** If there is no internet connection, an error message should appear.

---

## T3 - User Data Correction

**Description:** The user can review and correct the recognized data from the document.

- **Preconditions:** The document has been successfully displayed.
- **Steps:**
  1. Launch the application.
  2. Enter the name and select the CIPS document type.
  3. Upload the document and click submit.
  4. Modify some information in the fields.
  5. Click the finalize button.
- **Expected Result:** The modified data is successfully saved.

---
