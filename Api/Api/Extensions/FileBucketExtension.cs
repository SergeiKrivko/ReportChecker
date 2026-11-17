using ReportChecker.Abstractions;
using ReportChecker.Api.Dtos;

namespace ReportChecker.Api.Extensions;

public static class FileBucketExtension
{
    public static bool TryGetS3Bucket(this FileBucketDto dto, out FileRepositoryBucket s3Bucket)
    {
        switch (dto)
        {
            case FileBucketDto.Default:
                s3Bucket = FileRepositoryBucket.Sources;
                return true;
            case FileBucketDto.Local:
                s3Bucket = FileRepositoryBucket.LocalSources;
                return true;
            default:
                s3Bucket = FileRepositoryBucket.Sources;
                return false;
        }
    }
}
