using System.IO;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;

namespace Senstay.Dojo.Data.AWS
{
    public class AwsFile
    {
        private static IAmazonS3 _s3Client;

        public AwsFile() : this(true)
        {
        }

        public AwsFile(bool enableVersioning)
        {
            SetVersioning(enableVersioning);
        }

        private void SetVersioning(bool enableVersioning)
        {
            VersionStatus status = enableVersioning ? VersionStatus.Enabled : VersionStatus.Off;
            _s3Client.PutBucketVersioning(new PutBucketVersioningRequest
            {
                BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                VersioningConfig = new S3BucketVersioningConfig() { Status = status }
            });
        }

        public void AddFile(string filename, string content)
        {
            _s3Client.PutObject(new PutObjectRequest
            {
                BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                Key = filename,
                ContentBody = content
            });
        }

        public string ReadFile(string filename, string versionId = null)
        {
            var listResponse = _s3Client.ListVersions(new ListVersionsRequest
            {
                BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                Prefix = filename
            });

            if (string.IsNullOrEmpty(versionId))
            {
                var deleteMarkerVersion = listResponse.Versions.FirstOrDefault(x => x.IsDeleteMarker && x.IsLatest);
                if (deleteMarkerVersion != null) versionId = deleteMarkerVersion.VersionId;
            }

            if (!string.IsNullOrEmpty(versionId))
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                    Key = filename,
                    VersionId = versionId
                };

                using (GetObjectResponse getResponse = _s3Client.GetObject(getRequest))
                {
                    using (StreamReader reader = new StreamReader(getResponse.ResponseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return string.Empty;
        }

        public List<S3ObjectVersion> ListVersions(string filename)
        {
            var listResponse = _s3Client.ListVersions(new ListVersionsRequest
            {
                BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                Prefix = filename
            });

            return listResponse.Versions;
        }

        public void RemoveFile(string filename, string versionId = null)
        {
            var listResponse = _s3Client.ListVersions(new ListVersionsRequest
            {
                BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                Prefix = filename
            });

            if (string.IsNullOrEmpty(versionId))
            {
                var deleteMarkerVersion = listResponse.Versions.FirstOrDefault(x => x.IsDeleteMarker && x.IsLatest);
                if (deleteMarkerVersion != null) versionId = deleteMarkerVersion.VersionId;
            }

            if (!string.IsNullOrEmpty(versionId))
            {
                _s3Client.DeleteObject(new DeleteObjectRequest
                {
                    BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                    Key = filename,
                    VersionId = versionId
                });
            }
        }

        public void RestoreFile(string filename, string versionId = null)
        {
            var listResponse = _s3Client.ListVersions(new ListVersionsRequest
            {
                BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                Prefix = filename
            });

            if (string.IsNullOrEmpty(versionId))
            {
                var deleteMarkerVersion = listResponse.Versions.FirstOrDefault(x => x.IsDeleteMarker && x.IsLatest);
                if (deleteMarkerVersion != null) versionId = deleteMarkerVersion.VersionId;
            }

            if (!string.IsNullOrEmpty(versionId))
            {
                _s3Client.DeleteObject(new DeleteObjectRequest
                {
                    BucketName = AwsConstants.VERSIONING_BUCKET_NAME,
                    Key = filename,
                    VersionId = versionId
                });
            }
        }
    }

}