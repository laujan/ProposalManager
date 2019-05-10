// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.Extensions.Configuration;

namespace ProjectSmartLink.Service
{
    public class EncryptionService : IEncryptService
    {
        private AES _decrypt;
        public EncryptionService(IConfiguration configuration)
        {
            _decrypt = new AES(configuration);
        }

        public string DecryptString(string cipherText)
        {
            return _decrypt.Decrypt(cipherText);
        }

        public string EncryptString(string plainText)
        {
            return _decrypt.Encrypt(plainText);
        }
    }
}
