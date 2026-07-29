using System;
using global::System.Collections;
using global::System.Security.Cryptography;
using global::System.Text;
using global::System.IO;
using iTextCore.text.pdf.crypto;
using Org.BouncyCastle.X509;

/*
 * $Id: PdfEncryption.cs,v 1.13 2007/06/14 20:01:48 psoares33 Exp $
 *
 * Copyright 2001-2006 Paulo Soares
 *
 * The contents of this file are subject to the Mozilla Public License Version 1.1
 * (the "License"); you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the License.
 *
 * The Original Code is 'iText, a free JAVA-PDF library'.
 *
 * The Initial Developer of the Original Code is Bruno Lowagie. Portions created by
 * the Initial Developer are Copyright (C) 1999, 2000, 2001, 2002 by Bruno Lowagie.
 * All Rights Reserved.
 * Co-Developer of the code is Paulo Soares. Portions created by the Co-Developer
 * are Copyright (C) 2000, 2001, 2002 by Paulo Soares. All Rights Reserved.
 *
 * Contributor(s): all the names of the contributors are added in the source code
 * where applicable.
 *
 * Alternatively, the contents of this file may be used under the terms of the
 * LGPL license (the "GNU LIBRARY GENERAL PUBLIC LICENSE"), in which case the
 * provisions of LGPL are applicable instead of those above.  If you wish to
 * allow use of your version of this file only under the terms of the LGPL
 * License and not to allow others to use your version of this file under
 * the MPL, indicate your decision by deleting the provisions above and
 * replace them with the notice and other provisions required by the LGPL.
 * If you do not delete the provisions above, a recipient may use your version
 * of this file under either the MPL or the GNU LIBRARY GENERAL PUBLIC LICENSE.
 *
 * This library is free software; you can redistribute it and/or modify it
 * under the terms of the MPL as stated above or under the terms of the GNU
 * Library General Public License as published by the Free Software Foundation;
 * either version 2 of the License, or any later version.
 *
 * This library is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Library general Public License for more
 * details.
 *
 * If you didn't download this code from the following link, you should check if
 * you aren't using an obsolete version:
 * http://www.lowagie.com/iText/
 */

namespace iTextCore.text.pdf {

/**
 *
 * @author  Paulo Soares (psoares@consiste.pt)
 */
public class PdfEncryption {

    public const int STANDARD_ENCRYPTION_40 = 2;
    public const int STANDARD_ENCRYPTION_128 = 3;
    public const int AES_128 = 4;
    public const int AES_256 = 5;

    private static byte[] pad = {
        (byte)0x28, (byte)0xBF, (byte)0x4E, (byte)0x5E, (byte)0x4E, (byte)0x75,
        (byte)0x8A, (byte)0x41, (byte)0x64, (byte)0x00, (byte)0x4E, (byte)0x56,
        (byte)0xFF, (byte)0xFA, (byte)0x01, (byte)0x08, (byte)0x2E, (byte)0x2E,
        (byte)0x00, (byte)0xB6, (byte)0xD0, (byte)0x68, (byte)0x3E, (byte)0x80,
        (byte)0x2F, (byte)0x0C, (byte)0xA9, (byte)0xFE, (byte)0x64, (byte)0x53,
        (byte)0x69, (byte)0x7A};
        
    private static readonly byte[] salt = {(byte)0x73, (byte)0x41, (byte)0x6c, (byte)0x54};
    internal static readonly byte[] metadataPad = {(byte)255,(byte)255,(byte)255,(byte)255};
    /** The encryption key for a particular object/generation */
    internal byte[] key;
    /** The encryption key length for a particular object/generation */
    internal int keySize;
    /** The global encryption key */
    internal byte[] mkey;
    /** Work area to prepare the object/generation bytes */
    internal byte[] extra = new byte[5];
    /** The message digest algorithm MD5 */
    internal MD5 md5;
    /** The encryption key for the owner */
    internal byte[] ownerKey = new byte[32];
    /** The encryption key for the user */
    internal byte[] userKey = new byte[32];
    /** The encrypted user key (R=6) */
    internal byte[] ueKey;
    /** The encrypted owner key (R=6) */
    internal byte[] oeKey;
    /** The encrypted permissions (R=6) */
    internal byte[] perms;
    /** The public key security handler for certificate encryption */
    protected PdfPublicKeySecurityHandler publicKeyHandler = null;
    internal int permissions;
    internal byte[] documentID;
    internal static long seq = DateTime.Now.Ticks + Environment.TickCount;
    private int revision;
    private ARCFOUREncryption rc4 = new ARCFOUREncryption();
    /** The generic key length. It may be 40 or 128. */
    private int keyLength;
    private bool encryptMetadata;
    /**
     * Indicates if the encryption is only necessary for embedded files.
     * @since 2.1.3
     */
    private bool embeddedFilesOnly;

    private int cryptoMode;
    
    public PdfEncryption() {
        md5 = new MD5CryptoServiceProvider();
        publicKeyHandler = new PdfPublicKeySecurityHandler();
    }

    public PdfEncryption(PdfEncryption enc) : this() {
        mkey = (byte[])enc.mkey.Clone();
        ownerKey = (byte[])enc.ownerKey.Clone();
        userKey = (byte[])enc.userKey.Clone();
        permissions = enc.permissions;
        if (enc.documentID != null)
            documentID = (byte[])enc.documentID.Clone();
        revision = enc.revision;
        keyLength = enc.keyLength;
        encryptMetadata = enc.encryptMetadata;
        embeddedFilesOnly = enc.embeddedFilesOnly;
        publicKeyHandler = enc.publicKeyHandler;
    }

    public void SetCryptoMode(int mode, int kl) {
        cryptoMode = mode;
        encryptMetadata = (mode & PdfWriter.DO_NOT_ENCRYPT_METADATA) == 0;
        embeddedFilesOnly = (mode & PdfWriter.EMBEDDED_FILES_ONLY) != 0;
        mode &= PdfWriter.ENCRYPTION_MASK;
        switch (mode) {
            case PdfWriter.STANDARD_ENCRYPTION_40:
                encryptMetadata = true;
                embeddedFilesOnly = false;
                keyLength = 40;
                revision = STANDARD_ENCRYPTION_40;
                break;
            case PdfWriter.STANDARD_ENCRYPTION_128:
                embeddedFilesOnly = false;
                if (kl > 0)
                    keyLength = kl;
                else
                    keyLength = 128;
                revision = STANDARD_ENCRYPTION_128;
                break;
            case PdfWriter.ENCRYPTION_AES_128:
                keyLength = 128;
                revision = AES_128;
                break;
            case PdfWriter.ENCRYPTION_AES_256:
                keyLength = 256;
                revision = AES_256;
                break;
            default:
                throw new ArgumentException("No valid encryption mode");
        }
    }
    
    public int GetCryptoMode() {
        return cryptoMode;
    }
    
    public bool IsMetadataEncrypted() {
        return encryptMetadata;
    }

    /**
     * Indicates if only the embedded files have to be encrypted.
     * @return  if true only the embedded files will be encrypted
     * @since   2.1.3
     */
    public bool IsEmbeddedFilesOnly() {
        return embeddedFilesOnly;
    }

    private byte[] PadPassword(byte[] userPassword) {
        byte[] userPad = new byte[32];
        if (userPassword == null) {
            Array.Copy(pad, 0, userPad, 0, 32);
        }
        else {
            Array.Copy(userPassword, 0, userPad, 0, Math.Min(userPassword.Length, 32));
            if (userPassword.Length < 32)
                Array.Copy(pad, 0, userPad, userPassword.Length, 32 - userPassword.Length);
        }

        return userPad;
    }

    /**
     */
    private byte[] ComputeOwnerKey(byte[] userPad, byte[] ownerPad) {
        byte[] ownerKey = new byte[32];

        byte[] digest = md5.ComputeHash(ownerPad);
        if (revision == STANDARD_ENCRYPTION_128 || revision == AES_128) {
            byte[] mkey = new byte[keyLength / 8];
            // only use for the input as many bit as the key consists of
            for (int k = 0; k < 50; ++k)
                Array.Copy(md5.ComputeHash(digest), 0, digest, 0, mkey.Length);
            Array.Copy(userPad, 0, ownerKey, 0, 32);
            for (int i = 0; i < 20; ++i) {
                for (int j = 0; j < mkey.Length ; ++j)
                    mkey[j] = (byte)(digest[j] ^ i);
                rc4.PrepareARCFOURKey(mkey);
                rc4.EncryptARCFOUR(ownerKey);
            }
        }
        else {
            rc4.PrepareARCFOURKey(digest, 0, 5);
            rc4.EncryptARCFOUR(userPad, ownerKey);
        }

        return ownerKey;
    }

    /**
     *
     * ownerKey, documentID must be setuped
     */
    private void SetupGlobalEncryptionKey(byte[] documentID, byte[] userPad, byte[] ownerKey, int permissions) {
        this.documentID = documentID;
        this.ownerKey = ownerKey;
        this.permissions = permissions;
        // use variable keylength
        mkey = new byte[keyLength / 8];

        //fixed by ujihara in order to follow PDF refrence
        md5.Initialize();
        md5.TransformBlock(userPad, 0, userPad.Length, userPad, 0);
        md5.TransformBlock(ownerKey, 0, ownerKey.Length, ownerKey, 0);

        byte[] ext = new byte[4];
        ext[0] = (byte)permissions;
        ext[1] = (byte)(permissions >> 8);
        ext[2] = (byte)(permissions >> 16);
        ext[3] = (byte)(permissions >> 24);
        md5.TransformBlock(ext, 0, 4, ext, 0);
        if (documentID != null) 
            md5.TransformBlock(documentID, 0, documentID.Length, documentID, 0);
        if (!encryptMetadata)
            md5.TransformBlock(metadataPad, 0, metadataPad.Length, metadataPad, 0);
        md5.TransformFinalBlock(ext, 0, 0);

        byte[] digest = new byte[mkey.Length];
        Array.Copy(md5.Hash, 0, digest, 0, mkey.Length);

        
        md5.Initialize();
        // only use the really needed bits as input for the hash
        if (revision == STANDARD_ENCRYPTION_128 || revision == AES_128) {
            for (int k = 0; k < 50; ++k) {
                Array.Copy(md5.ComputeHash(digest), 0, digest, 0, mkey.Length);
                md5.Initialize();
            }
        }
        Array.Copy(digest, 0, mkey, 0, mkey.Length);
    }

    /**
     *
     * mkey must be setuped
     */
    // use the revision to choose the setup method
    private void SetupUserKey() {
        if (revision == STANDARD_ENCRYPTION_128 || revision == AES_128) {
            md5.TransformBlock(pad, 0, pad.Length, pad, 0);
            md5.TransformFinalBlock(documentID, 0, documentID.Length);
            byte[] digest = md5.Hash;
            md5.Initialize();
            Array.Copy(digest, 0, userKey, 0, 16);
            for (int k = 16; k < 32; ++k)
                userKey[k] = 0;
            for (int i = 0; i < 20; ++i) {
                for (int j = 0; j < mkey.Length; ++j)
                    digest[j] = (byte)(mkey[j] ^ i);
                rc4.PrepareARCFOURKey(digest, 0, mkey.Length);
                rc4.EncryptARCFOUR(userKey, 0, 16);
            }
        }
        else {
            rc4.PrepareARCFOURKey(mkey);
            rc4.EncryptARCFOUR(pad, userKey);
        }
    }

    // gets keylength and revision and uses revison to choose the initial values for permissions
    public void SetupAllKeys(byte[] userPassword, byte[] ownerPassword, int permissions) {
        if (revision == AES_256) {
            // R=6 uses completely different key derivation (SHA-256 based)
            SetupAllKeysR6(userPassword, ownerPassword, permissions);
            return;
        }
        if (ownerPassword == null || ownerPassword.Length == 0)
            ownerPassword = md5.ComputeHash(CreateDocumentId());
        md5.Initialize();
        permissions |= (int)((revision == STANDARD_ENCRYPTION_128 || revision == AES_128) ? (uint)0xfffff0c0 : (uint)0xffffffc0);
        permissions &= unchecked((int)0xfffffffc);
        //PDF refrence 3.5.2 Standard Security Handler, Algorithum 3.3-1
        //If there is no owner password, use the user password instead.
        byte[] userPad = PadPassword(userPassword);
        byte[] ownerPad = PadPassword(ownerPassword);

        this.ownerKey = ComputeOwnerKey(userPad, ownerPad);
        documentID = CreateDocumentId();
        SetupByUserPad(this.documentID, userPad, this.ownerKey, permissions);
    }

    public static byte[] CreateDocumentId() {
        MD5 md5 = new MD5CryptoServiceProvider();
        long time = DateTime.Now.Ticks + Environment.TickCount;
        long mem = GC.GetTotalMemory(false);
        String s = time + "+" + mem + "+" + (seq++);
        return md5.ComputeHash(Encoding.ASCII.GetBytes(s));
    }

    public void SetupByUserPassword(byte[] documentID, byte[] userPassword, byte[] ownerKey, int permissions) {
        SetupByUserPad(documentID, PadPassword(userPassword), ownerKey, permissions);
    }

    /**
     */
    private void SetupByUserPad(byte[] documentID, byte[] userPad, byte[] ownerKey, int permissions) {
        SetupGlobalEncryptionKey(documentID, userPad, ownerKey, permissions);
        SetupUserKey();
    }

    /**
     */
    public void SetupByOwnerPassword(byte[] documentID, byte[] ownerPassword, byte[] userKey, byte[] ownerKey, int permissions) {
        SetupByOwnerPad(documentID, PadPassword(ownerPassword), userKey, ownerKey, permissions);
    }

    private void SetupByOwnerPad(byte[] documentID, byte[] ownerPad, byte[] userKey, byte[] ownerKey, int permissions) {
        byte[] userPad = ComputeOwnerKey(ownerKey, ownerPad); //userPad will be set in this.ownerKey
        SetupGlobalEncryptionKey(documentID, userPad, ownerKey, permissions); //step 3
        SetupUserKey();
    }

    public void SetupByEncryptionKey(byte[] key, int keylength) {
        mkey = new byte[keylength/8];
        global::System.Array.Copy(key, 0, mkey, 0, mkey.Length);
    }    

    public void SetHashKey(int number, int generation) {
        if (revision == AES_256) {
            // AES-256: use the file encryption key directly (no per-object key derivation)
            key = mkey;
            keySize = 32;
            return;
        }
        md5.Initialize();    //added by ujihara
        extra[0] = (byte)number;
        extra[1] = (byte)(number >> 8);
        extra[2] = (byte)(number >> 16);
        extra[3] = (byte)generation;
        extra[4] = (byte)(generation >> 8);
        md5.TransformBlock(mkey, 0, mkey.Length, mkey, 0);
        md5.TransformBlock(extra, 0, extra.Length, extra, 0);
        if (revision == AES_128)
            md5.TransformBlock(salt, 0, salt.Length, salt, 0);
        md5.TransformFinalBlock(extra, 0, 0);
        key = md5.Hash;
        md5.Initialize();
        keySize = mkey.Length + 5;
        if (keySize > 16)
            keySize = 16;
    }

    public static PdfObject CreateInfoId(byte[] id) {
        ByteBuffer buf = new ByteBuffer(90);
        buf.Append('[').Append('<');
        for (int k = 0; k < 16; ++k)
            buf.AppendHex(id[k]);
        buf.Append('>').Append('<');
        id = CreateDocumentId();
        for (int k = 0; k < 16; ++k)
            buf.AppendHex(id[k]);
        buf.Append('>').Append(']');
        return new PdfLiteral(buf.ToByteArray());
    }

    public PdfDictionary GetEncryptionDictionary() {
        PdfDictionary dic = new PdfDictionary();
        
        if (publicKeyHandler.GetRecipientsSize() > 0) {
            PdfArray recipients = null;
            
            dic.Put(PdfName.FILTER, PdfName.PUBSEC);  
            dic.Put(PdfName.R, new PdfNumber(revision));	

            recipients = publicKeyHandler.GetEncodedRecipients();
            
            if (revision == STANDARD_ENCRYPTION_40) {
                dic.Put(PdfName.V, new PdfNumber(1));
                dic.Put(PdfName.SUBFILTER, PdfName.ADBE_PKCS7_S4);
                dic.Put(PdfName.RECIPIENTS, recipients);
            }
            else if (revision == STANDARD_ENCRYPTION_128 && encryptMetadata) {
                dic.Put(PdfName.V, new PdfNumber(2));
                dic.Put(PdfName.LENGTH, new PdfNumber(128));
                dic.Put(PdfName.SUBFILTER, PdfName.ADBE_PKCS7_S4);
                dic.Put(PdfName.RECIPIENTS, recipients);
            }
            else {               
                dic.Put(PdfName.R, new PdfNumber(AES_128));
                dic.Put(PdfName.V, new PdfNumber(4));
                dic.Put(PdfName.SUBFILTER, PdfName.ADBE_PKCS7_S5);
                                
                PdfDictionary stdcf = new PdfDictionary();
                stdcf.Put(PdfName.RECIPIENTS, recipients);                                                                    
                if (!encryptMetadata)
                    stdcf.Put(PdfName.ENCRYPTMETADATA, PdfBoolean.PDFFALSE);
                
                if (revision == AES_128)
                    stdcf.Put(PdfName.CFM, PdfName.AESV2);
                else
                    stdcf.Put(PdfName.CFM, PdfName.V2);                  
                PdfDictionary cf = new PdfDictionary();
                cf.Put(PdfName.DEFAULTCRYPTFILTER, stdcf);                
                dic.Put(PdfName.CF, cf);
                if (embeddedFilesOnly) {
                    dic.Put(PdfName.EFF, PdfName.DEFAULTCRYPTFILTER);
                    dic.Put(PdfName.STRF, PdfName.IDENTITY);
                    dic.Put(PdfName.STMF, PdfName.IDENTITY);
                }
                else {
                    dic.Put(PdfName.STRF, PdfName.DEFAULTCRYPTFILTER);
                    dic.Put(PdfName.STMF, PdfName.DEFAULTCRYPTFILTER);
                }
            }
            
            SHA1 sh = new SHA1CryptoServiceProvider();
            byte[] encodedRecipient = null;
            byte[] seed = publicKeyHandler.GetSeed();
            sh.TransformBlock(seed, 0, seed.Length, seed, 0);
            for (int i=0; i<publicKeyHandler.GetRecipientsSize(); i++)
            {
                encodedRecipient = publicKeyHandler.GetEncodedRecipient(i);
                sh.TransformBlock(encodedRecipient, 0, encodedRecipient.Length, encodedRecipient, 0);
            }
            if (!encryptMetadata)
                sh.TransformBlock(metadataPad, 0, metadataPad.Length, metadataPad, 0);
            sh.TransformFinalBlock(seed, 0, 0);        
            byte[] mdResult = sh.Hash;
            
            SetupByEncryptionKey(mdResult, keyLength);              
        } else {
            dic.Put(PdfName.FILTER, PdfName.STANDARD);
            dic.Put(PdfName.O, new PdfLiteral(PdfContentByte.EscapeString(ownerKey)));
            dic.Put(PdfName.U, new PdfLiteral(PdfContentByte.EscapeString(userKey)));
            dic.Put(PdfName.P, new PdfNumber(permissions));
            dic.Put(PdfName.R, new PdfNumber(revision));
            if (revision == STANDARD_ENCRYPTION_40) {
                dic.Put(PdfName.V, new PdfNumber(1));
            }
            else if (revision == STANDARD_ENCRYPTION_128 && encryptMetadata) {
                dic.Put(PdfName.V, new PdfNumber(2));
                dic.Put(PdfName.LENGTH, new PdfNumber(128));
                
            }
            else if (revision == AES_256) {
                // AES-256 (R=6, V=5)
                if (!encryptMetadata)
                    dic.Put(PdfName.ENCRYPTMETADATA, PdfBoolean.PDFFALSE);
                dic.Put(PdfName.R, new PdfNumber(AES_256));
                dic.Put(PdfName.V, new PdfNumber(5));
                dic.Put(PdfName.LENGTH, new PdfNumber(256));
                dic.Put(PdfName.O, new PdfLiteral(PdfContentByte.EscapeString(ownerKey)));
                dic.Put(PdfName.U, new PdfLiteral(PdfContentByte.EscapeString(userKey)));
                dic.Put(PdfName.OE, new PdfLiteral(PdfContentByte.EscapeString(oeKey)));
                dic.Put(PdfName.UE, new PdfLiteral(PdfContentByte.EscapeString(ueKey)));
                dic.Put(PdfName.PERMS, new PdfLiteral(PdfContentByte.EscapeString(perms)));
                PdfDictionary stdcf256 = new PdfDictionary();
                stdcf256.Put(PdfName.LENGTH, new PdfNumber(32));
                stdcf256.Put(PdfName.AUTHEVENT, PdfName.DOCOPEN);
                stdcf256.Put(PdfName.CFM, PdfName.AESV3);
                PdfDictionary cf256 = new PdfDictionary();
                cf256.Put(PdfName.STDCF, stdcf256);
                dic.Put(PdfName.CF, cf256);
                dic.Put(PdfName.STRF, PdfName.STDCF);
                dic.Put(PdfName.STMF, PdfName.STDCF);
            }
            else {
                if (!encryptMetadata)
                    dic.Put(PdfName.ENCRYPTMETADATA, PdfBoolean.PDFFALSE);
                dic.Put(PdfName.R, new PdfNumber(AES_128));
                dic.Put(PdfName.V, new PdfNumber(4));
                dic.Put(PdfName.LENGTH, new PdfNumber(128));
                PdfDictionary stdcf = new PdfDictionary();
                stdcf.Put(PdfName.LENGTH, new PdfNumber(16));
                if (embeddedFilesOnly) {
                    stdcf.Put(PdfName.AUTHEVENT, PdfName.EFOPEN);
                    dic.Put(PdfName.EFF, PdfName.STDCF);
                    dic.Put(PdfName.STRF, PdfName.IDENTITY);
                    dic.Put(PdfName.STMF, PdfName.IDENTITY);
                }
                else {
                    stdcf.Put(PdfName.AUTHEVENT, PdfName.DOCOPEN);
                    dic.Put(PdfName.STRF, PdfName.STDCF);
                    dic.Put(PdfName.STMF, PdfName.STDCF);
                }
                if (revision == AES_128)
                    stdcf.Put(PdfName.CFM, PdfName.AESV2);
                else
                    stdcf.Put(PdfName.CFM, PdfName.V2);
                PdfDictionary cf = new PdfDictionary();
                cf.Put(PdfName.STDCF, stdcf);
                dic.Put(PdfName.CF, cf);
            }
        }
        return dic;
    }
    
    public PdfObject FileID {
        get {
            return CreateInfoId(documentID);
        }
    }
    public OutputStreamEncryption GetEncryptionStream(Stream os) {
        return new OutputStreamEncryption(os, key, 0, keySize, revision);
    }
    
    public int CalculateStreamSize(int n) {
        if (revision == AES_128 || revision == AES_256)
            return (n & 0x7ffffff0) + 32;
        else
            return n;
    }
    
    public byte[] EncryptByteArray(byte[] b) {
        MemoryStream ba = new MemoryStream();
        OutputStreamEncryption os2 = GetEncryptionStream(ba);
        os2.Write(b, 0, b.Length);
        os2.Finish();
        return ba.ToArray();
    }
    
    public StandardDecryption GetDecryptor() {
        return new StandardDecryption(key, 0, keySize, revision);
    }
    
    public byte[] DecryptByteArray(byte[] b) {
        MemoryStream ba = new MemoryStream();
        StandardDecryption dec = GetDecryptor();
        byte[] b2 = dec.Update(b, 0, b.Length);
        if (b2 != null)
            ba.Write(b2, 0, b2.Length);
        b2 = dec.Finish();
        if (b2 != null)
            ba.Write(b2, 0, b2.Length);
        return ba.ToArray();
    }

    public void AddRecipient(X509Certificate cert, int permission) {
        documentID = CreateDocumentId();
        publicKeyHandler.AddRecipient(new PdfPublicKeyRecipient(cert, permission));
    }

	public byte[] ComputeUserPassword(byte[] ownerPassword) {
		byte[] userPad = ComputeOwnerKey(ownerKey, PadPassword(ownerPassword));
		for (int i = 0; i < userPad.Length; i++) {
			bool match = true;
			for (int j = 0; j < userPad.Length - i; j++) {
				if (userPad[i + j] != pad[j]) {
					match = false;
					break;
                }
			}
			if (!match) continue;
			byte[] userPassword = new byte[i];
			global::System.Array.Copy(userPad, 0, userPassword, 0, i);
			return userPassword;
		}
		return userPad;
	}

    // ===================== AES-256 (R=6) Support =====================

    /// <summary>
    /// ISO 32000-2, Algorithm 2.B — Computing a hash (revision 6).
    /// Iterative SHA-256/384/512 hash used for password validation and key derivation.
    /// </summary>
    internal static byte[] ComputeHashR6(byte[] password, byte[] salt, byte[] userKey) {
        using (var sha256 = global::System.Security.Cryptography.SHA256.Create())
        using (var sha384 = global::System.Security.Cryptography.SHA384.Create())
        using (var sha512 = global::System.Security.Cryptography.SHA512.Create()) {
            // Step a: SHA-256 of password + salt + userKey
            sha256.Initialize();
            sha256.TransformBlock(password, 0, password.Length, password, 0);
            sha256.TransformBlock(salt, 0, salt.Length, salt, 0);
            if (userKey != null)
                sha256.TransformBlock(userKey, 0, userKey.Length, userKey, 0);
            sha256.TransformFinalBlock(new byte[0], 0, 0);
            byte[] k = sha256.Hash;

            // Step b-e: iterate
            int round = 0;
            while (true) {
                // Step b: build K1 = password + K + userKey, repeated 64 times
                int sequenceLen = password.Length + k.Length + (userKey != null ? userKey.Length : 0);
                byte[] k1 = new byte[sequenceLen * 64];
                for (int i = 0; i < 64; i++) {
                    int offset = i * sequenceLen;
                    global::System.Array.Copy(password, 0, k1, offset, password.Length);
                    offset += password.Length;
                    global::System.Array.Copy(k, 0, k1, offset, k.Length);
                    offset += k.Length;
                    if (userKey != null)
                        global::System.Array.Copy(userKey, 0, k1, offset, userKey.Length);
                }

                // Step c: AES-128-CBC encrypt K1 with first 16 bytes of K as key, next 16 as IV
                byte[] aesKey = new byte[16];
                byte[] aesIv = new byte[16];
                global::System.Array.Copy(k, 0, aesKey, 0, 16);
                global::System.Array.Copy(k, 16, aesIv, 0, 16);

                byte[] e;
                using (var aes = global::System.Security.Cryptography.Aes.Create()) {
                    aes.Mode = global::System.Security.Cryptography.CipherMode.CBC;
                    aes.Padding = global::System.Security.Cryptography.PaddingMode.None;
                    aes.Key = aesKey;
                    aes.IV = aesIv;
                    using (var enc = aes.CreateEncryptor()) {
                        e = enc.TransformFinalBlock(k1, 0, k1.Length);
                    }
                }

                // Step d: take the first 16 bytes of E, interpret as big-endian 128-bit int, mod 3
                // determines which SHA to use. We only need the last byte mod 3 for the selection.
                int bigIntMod3 = BigIntMod3(e);

                // Step e: hash E with the selected SHA
                switch (bigIntMod3) {
                    case 0:
                        k = sha256.ComputeHash(e);
                        sha256.Initialize();
                        break;
                    case 1:
                        k = sha384.ComputeHash(e);
                        sha384.Initialize();
                        break;
                    default:
                        k = sha512.ComputeHash(e);
                        sha512.Initialize();
                        break;
                }

                // Step f: check termination — round >= 64 and last byte of E <= round - 32
                int lastE = e[e.Length - 1] & 0xFF;
                round++;
                if (round >= 64 && lastE <= round - 32)
                    break;
            }

            // Return first 32 bytes
            byte[] result = new byte[32];
            global::System.Array.Copy(k, 0, result, 0, 32);
            return result;
        }
    }

    /// <summary>
    /// Compute big-endian 128-bit integer from first 16 bytes of data, modulo 3.
    /// </summary>
    private static int BigIntMod3(byte[] data) {
        int rem = 0;
        for (int i = 0; i < 16 && i < data.Length; i++) {
            rem = (rem * 256 + (data[i] & 0xFF)) % 3;
        }
        return rem;
    }

    /// <summary>
    /// Validate the user password for R=6 encryption.
    /// ISO 32000-2, Algorithm 2.A (validation) — Section 7.6.4.3.3
    /// </summary>
    public bool IsUserPasswordR6(byte[] password, byte[] u, byte[] ue) {
        if (password == null) password = new byte[0];
        if (password.Length > 127) {
            byte[] trimmed = new byte[127];
            global::System.Array.Copy(password, 0, trimmed, 0, 127);
            password = trimmed;
        }

        // User Validation Salt = U[32..39]
        byte[] validationSalt = new byte[8];
        global::System.Array.Copy(u, 32, validationSalt, 0, 8);

        byte[] hash = ComputeHashR6(password, validationSalt, null);

        // Compare first 32 bytes of U with hash
        for (int i = 0; i < 32; i++) {
            if (u[i] != hash[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Validate the owner password for R=6 encryption.
    /// ISO 32000-2, Algorithm 2.A (validation) — Section 7.6.4.3.3
    /// </summary>
    public bool IsOwnerPasswordR6(byte[] password, byte[] o, byte[] oe, byte[] u) {
        if (password == null) password = new byte[0];
        if (password.Length > 127) {
            byte[] trimmed = new byte[127];
            global::System.Array.Copy(password, 0, trimmed, 0, 127);
            password = trimmed;
        }

        // Owner Validation Salt = O[32..39]
        byte[] validationSalt = new byte[8];
        global::System.Array.Copy(o, 32, validationSalt, 0, 8);

        // U value (48 bytes) is used in owner hash
        byte[] hash = ComputeHashR6(password, validationSalt, u);

        // Compare first 32 bytes of O with hash
        for (int i = 0; i < 32; i++) {
            if (o[i] != hash[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Compute the file encryption key from user password for R=6.
    /// ISO 32000-2, Algorithm 2.A (key recovery) — Section 7.6.4.3.3
    /// </summary>
    public byte[] ComputeFileKeyFromUserR6(byte[] password, byte[] u, byte[] ue) {
        if (password == null) password = new byte[0];
        if (password.Length > 127) {
            byte[] trimmed = new byte[127];
            global::System.Array.Copy(password, 0, trimmed, 0, 127);
            password = trimmed;
        }

        // User Key Salt = U[40..47]
        byte[] keySalt = new byte[8];
        global::System.Array.Copy(u, 40, keySalt, 0, 8);

        byte[] key2 = ComputeHashR6(password, keySalt, null);

        // Decrypt UE with AES-256-CBC, key = hash, IV = all zeros
        return DecryptAes256Cbc(key2, ue);
    }

    /// <summary>
    /// Compute the file encryption key from owner password for R=6.
    /// ISO 32000-2, Algorithm 2.A (key recovery) — Section 7.6.4.3.3
    /// </summary>
    public byte[] ComputeFileKeyFromOwnerR6(byte[] password, byte[] o, byte[] oe, byte[] u) {
        if (password == null) password = new byte[0];
        if (password.Length > 127) {
            byte[] trimmed = new byte[127];
            global::System.Array.Copy(password, 0, trimmed, 0, 127);
            password = trimmed;
        }

        // Owner Key Salt = O[40..47]
        byte[] keySalt = new byte[8];
        global::System.Array.Copy(o, 40, keySalt, 0, 8);

        byte[] key2 = ComputeHashR6(password, keySalt, u);

        // Decrypt OE with AES-256-CBC, key = hash, IV = all zeros
        return DecryptAes256Cbc(key2, oe);
    }

    /// <summary>
    /// AES-256-CBC encryption with zero IV. Used for UE/OE key wrapping.
    /// </summary>
    private static byte[] EncryptAes256Cbc(byte[] key, byte[] data) {
        using (var aes = global::System.Security.Cryptography.Aes.Create()) {
            aes.Mode = global::System.Security.Cryptography.CipherMode.CBC;
            aes.Padding = global::System.Security.Cryptography.PaddingMode.None;
            aes.Key = key;
            aes.IV = new byte[16]; // all zeros
            using (var enc = aes.CreateEncryptor()) {
                return enc.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }

    /// <summary>
    /// ISO 32000-2, Algorithm 2.C — Compute the Perms value for R=6.
    /// </summary>
    private byte[] ComputePermsR6(int permissions, bool encryptMeta) {
        byte[] permsData = new byte[16];
        // Bytes 0-3: permissions as unsigned 32-bit little-endian
        permsData[0] = (byte)permissions;
        permsData[1] = (byte)(permissions >> 8);
        permsData[2] = (byte)(permissions >> 16);
        permsData[3] = (byte)(permissions >> 24);
        // Bytes 4-7: 0xFF if encrypting metadata, 0x54 ('T') otherwise
        permsData[4] = encryptMeta ? (byte)0xFF : (byte)0x54;
        permsData[5] = encryptMeta ? (byte)0xFF : (byte)0x54;
        permsData[6] = encryptMeta ? (byte)0xFF : (byte)0x54;
        permsData[7] = encryptMeta ? (byte)0xFF : (byte)0x54;
        // Bytes 8-11: 'adb1' marker
        permsData[8] = (byte)'a';
        permsData[9] = (byte)'d';
        permsData[10] = (byte)'b';
        permsData[11] = (byte)'1';
        // Bytes 12-15: random
        byte[] rand4 = new byte[4];
        using (var rng = new global::System.Security.Cryptography.RNGCryptoServiceProvider())
            rng.GetBytes(rand4);
        global::System.Array.Copy(rand4, 0, permsData, 12, 4);

        // Encrypt with AES-256-ECB (no padding) using the file encryption key
        using (var aes = global::System.Security.Cryptography.Aes.Create()) {
            aes.Mode = global::System.Security.Cryptography.CipherMode.ECB;
            aes.Padding = global::System.Security.Cryptography.PaddingMode.None;
            aes.Key = mkey;
            using (var enc = aes.CreateEncryptor()) {
                return enc.TransformFinalBlock(permsData, 0, 16);
            }
        }
    }

    /// <summary>
    /// Generate all encryption keys for R=6 (AES-256) from user and owner passwords.
    /// ISO 32000-2, Section 7.6.4.3.3 — Algorithms for key generation.
    /// </summary>
    public void SetupAllKeysR6(byte[] userPassword, byte[] ownerPassword, int permissions) {
        if (userPassword == null) userPassword = new byte[0];
        if (ownerPassword == null || ownerPassword.Length == 0)
            ownerPassword = userPassword;
        // Truncate passwords to 127 bytes (UTF-8)
        if (userPassword.Length > 127) {
            byte[] t = new byte[127];
            global::System.Array.Copy(userPassword, 0, t, 0, 127);
            userPassword = t;
        }
        if (ownerPassword.Length > 127) {
            byte[] t = new byte[127];
            global::System.Array.Copy(ownerPassword, 0, t, 0, 127);
            ownerPassword = t;
        }

        this.permissions = permissions | unchecked((int)0xfffff000);
        this.permissions &= unchecked((int)0xfffffffc);

        // Generate random 32-byte file encryption key
        mkey = new byte[32];
        using (var rng = new global::System.Security.Cryptography.RNGCryptoServiceProvider())
            rng.GetBytes(mkey);

        // Generate random salts (8 bytes each: validation salt + key salt)
        byte[] userValSalt = new byte[8];
        byte[] userKeySalt = new byte[8];
        byte[] ownerValSalt = new byte[8];
        byte[] ownerKeySalt = new byte[8];
        using (var rng = new global::System.Security.Cryptography.RNGCryptoServiceProvider()) {
            rng.GetBytes(userValSalt);
            rng.GetBytes(userKeySalt);
            rng.GetBytes(ownerValSalt);
            rng.GetBytes(ownerKeySalt);
        }

        // Compute U (48 bytes) = hash(pwd, valSalt) + valSalt + keySalt
        byte[] userHash = ComputeHashR6(userPassword, userValSalt, null);
        userKey = new byte[48];
        global::System.Array.Copy(userHash, 0, userKey, 0, 32);
        global::System.Array.Copy(userValSalt, 0, userKey, 32, 8);
        global::System.Array.Copy(userKeySalt, 0, userKey, 40, 8);

        // Compute UE (32 bytes) = AES-256-CBC-encrypt(fileKey) with hash(pwd, keySalt) as key
        byte[] userKeyForUE = ComputeHashR6(userPassword, userKeySalt, null);
        ueKey = EncryptAes256Cbc(userKeyForUE, mkey);

        // Compute O (48 bytes) = hash(pwd, valSalt, U) + valSalt + keySalt
        byte[] ownerHash = ComputeHashR6(ownerPassword, ownerValSalt, userKey);
        ownerKey = new byte[48];
        global::System.Array.Copy(ownerHash, 0, ownerKey, 0, 32);
        global::System.Array.Copy(ownerValSalt, 0, ownerKey, 32, 8);
        global::System.Array.Copy(ownerKeySalt, 0, ownerKey, 40, 8);

        // Compute OE (32 bytes) = AES-256-CBC-encrypt(fileKey) with hash(pwd, keySalt, U) as key
        byte[] ownerKeyForOE = ComputeHashR6(ownerPassword, ownerKeySalt, userKey);
        oeKey = EncryptAes256Cbc(ownerKeyForOE, mkey);

        // Compute Perms (16 bytes)
        perms = ComputePermsR6(this.permissions, encryptMetadata);

        // Set document ID
        documentID = CreateDocumentId();
    }

    /// <summary>
    /// AES-256-CBC decryption with zero IV. Used for UE/OE key unwrapping.
    /// </summary>
    private static byte[] DecryptAes256Cbc(byte[] key, byte[] data) {
        using (var aes = global::System.Security.Cryptography.Aes.Create()) {
            aes.Mode = global::System.Security.Cryptography.CipherMode.CBC;
            aes.Padding = global::System.Security.Cryptography.PaddingMode.None;
            aes.Key = key;
            aes.IV = new byte[16]; // all zeros
            using (var dec = aes.CreateDecryptor()) {
                return dec.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }

    /// <summary>
    /// Setup encryption for R=6 by verifying the password and extracting the file key.
    /// </summary>
    public bool SetupByPasswordR6(byte[] documentID, byte[] password, byte[] u, byte[] o,
                                   byte[] ue, byte[] oe, int permissions, bool encMeta) {
        this.documentID = documentID;
        this.permissions = permissions;
        this.encryptMetadata = encMeta;

        // Try user password first
        if (IsUserPasswordR6(password, u, ue)) {
            mkey = ComputeFileKeyFromUserR6(password, u, ue);
            return true;
        }

        // Try owner password
        if (IsOwnerPasswordR6(password, o, oe, u)) {
            mkey = ComputeFileKeyFromOwnerR6(password, o, oe, u);
            return true;
        }

        return false;
    }
}
}