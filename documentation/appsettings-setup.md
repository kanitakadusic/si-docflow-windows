# docflow-windows

## Application Settings Setup

> Variables marked with * must be properly set for the application to work correctly. Other variables can remain as they are in the [appsettings.json](../docflow/docflow/appsettings.json) file.

### ADMIN_SERVER_BASE_URL *

The base URL of the [admin server](https://github.com/HarisMalisevic/si-docflow-admin) used by the Windows application to fetch configuration and send logs.

> Running the admin server is optional - the application can work without it, but configuration will not be fetched and logs will not be sent.

### PROCESSING_SERVER_BASE_URL *

The base URL of the [processing server](https://github.com/kanitakadusic/si-docflow-server) used by the Windows application to handle document processing and finalization.

### PORT *

The port on which the Windows application listens for incoming document processing requests.

To support requests coming from the internet (outside the local network), it's necessary to:
1. Allow inbound traffic on this port in the system firewall or security software.
2. Configure port forwarding on the router to direct external traffic to the local machine on this port.

### MACHINE_ID *

A unique identifier for the Windows application used to fetch its configuration from the admin server. It follows the format **\<IP address\>:\<port\>**, where \<port\> should match the value of the `PORT` variable. Depending on the admin server's location, this variable should be set to one of the following values:

- Admin server is running on the same machine as the Windows application:
```
127.0.0.1:<port>
```

- Admin server is **not** running on the same machine as the Windows application:
```
<router’s public IP address>:<port>
```

### OPERATIONAL_MODE

Specifies the mode in which the Windows application will run if fetching the configuration from the admin server fails. There are two possible modes, shown below with their exact names:

- The application runs with the user interface (UI) enabled:
```
standalone
```

- The application runs without a UI but listens for document processing requests:
```
headless
```

### POLLING_FREQUENCY

Specifies the interval, in hours (integer), at which the application requests configuration updates from the admin server. This value can be updated if a different frequency is received from the admin server configuration.

### OCR_LANGUAGE

Specifies the language code (three-letter) used by the processing server for document processing, passed as a parameter to OCR engines that require it.

### OCR_ENGINE

Specifies which OCR engine the processing server will use to process documents. Supported values:

```
tesseract
```
```
googleVision
```
```
chatGpt
```
