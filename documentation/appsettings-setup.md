# docflow-windows

## Application Settings Setup

> The variables described in this document should be adjusted according to the intended configuration. All other variables should retain their default values.

### admin_api

The Windows application communicates with the admin server to retrieve configuration data, listen for document processing requests, and log significant actions. The `admin_api` variable holds the base URL of the admin server used for this communication.

### docflow_api

The Windows application communicates with the processing server to handle document processing and finalization. The `docflow_api` variable holds the base URL of the processing server used for this communication.

### machine_id

The `machine_id` variable serves as a unique identifier used by the Windows application to fetch its initial configuration from the admin server. It follows the format **ipAddress:port**.

### defaultPort

The `defaultPort` variable defines the port on which the Windows application listens for incoming document processing requests.

### lang

The `lang` variable specifies the language of documents to be processed. It uses a three-letter country code and is passed as a parameter during document processing.

### engine

The `engine` variable specifies the OCR engine to be used during document processing. Currently supported options are: _tesseract_, _googleVision_, and _chatGpt_.
