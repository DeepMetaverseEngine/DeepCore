// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC721IMLayout.sol";
import "../../nameserver/ownable/OwnableLogic.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../../nameserver/reentrancy/ReentrancyLogic.sol";
import "../erc721/HERC721Logic.sol";
import "./HERC721IMInterface.sol";

import "../../nameserver/nameService/NameServiceInterface.sol";
import "../assetVault/AssetVaultInterface.sol";
import "./HERC721IMType.sol";

/*
Auto Increasing TokenId and Specified TokenId could be adapted at same time,
The server offline could chose a very big number as offset.
*/
abstract contract HERC721IMLogic is HERC721IMLayout, OwnableLogic, NameServiceRefLogic, ReentrancyLogic, HERC721Logic, HERC721IMInterface {
    using Counters for Counters.Counter;

    using Strings for uint256;
    using SafeMath for uint256;
    using Address for address;
    using EnumerableSet for EnumerableSet.AddressSet;
    using EnumerableSet for EnumerableSet.UintSet;
    using EnumerableSet for EnumerableSet.Bytes32Set;

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoMint(){
        _sudoMint = true;
        _;
        _sudoMint = false;
    }

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoBurn(){
        _sudoBurn = true;
        _;
        _sudoBurn = false;
    }

    //for non-sudo mint, the "to" must be the msg.sender
    function _mintNormal(uint256 tokenId) internal onlyOnce returns (uint256){

        if (tokenId == 0) {
            //starts from 1
            _tokenIdCounter.increment();
            tokenId = _tokenIdCounter.current();
        }

        _mint(msg.sender, tokenId);
        emit Mint(msg.sender, tokenId);

        return tokenId;
    }

    //=========================================

    function _mintSudo(address to, uint256 tokenId) internal enableSudoMint onlyOnce returns (uint256){

        if (tokenId == 0) {
            //starts from 1
            _tokenIdCounter.increment();
            tokenId = _tokenIdCounter.current();
        }

        _mint(to, tokenId);
        emit Mint(to, tokenId);

        return tokenId;
    }

    //=========================================

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnNormal(uint256 tokenId) internal onlyOnce {
        require(_exists(tokenId), "tokenId does not exist");
        require(ownerOf(tokenId) == msg.sender, "can only burn tokenId belongs to yourself");

        _burn(tokenId);

        emit Burn(msg.sender, tokenId);

    }

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnSudo(uint256 tokenId) internal enableSudoBurn onlyOnce {
        require(_exists(tokenId), "tokenId does not exist");

        address owner = ownerOf(tokenId);
        _burn(tokenId);

        emit Burn(owner, tokenId);

    }

    //=========================================

    function _freezeNormal(uint256 tokenId, address unlocker) internal {
        require(_isApprovedOrOwner(msg.sender, tokenId), "_lock caller is not owner nor approved");
        _doLock(tokenId, unlocker);
        emit Freeze(unlocker, tokenId);
    }

    function _freezeSudo(uint256 tokenId, address unlocker) internal {
        _doLock(tokenId, unlocker);
        emit Freeze(unlocker, tokenId);
    }

    //=========================================

    function _thawNormal(uint256 tokenId) internal {
        _doUnlock(tokenId, msg.sender);
        emit Thaw(msg.sender, tokenId);
    }

    function _thawSudo(uint256 tokenId, address unlocker) internal {
        _doUnlock(tokenId, unlocker);
        emit Thaw(msg.sender, tokenId);
    }

    //=========================================

    function _setUint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 attributeValue) internal {
        bytes32[] memory attributeNames = new bytes32[](1);
        attributeNames[0] = attributeName;
        uint256[] memory attributeValues = new uint256[](1);
        attributeValues[0] = attributeValue;

        _setAttributes(attributeNames, tokenId, attributeValues, new bytes32[](0), new address[](0), new bytes[](0));
    }

    function _setUint256Attributes(bytes32[] memory attributeNames, uint256 tokenId, uint256[] memory attributeValues) internal {
        _setAttributes(attributeNames, tokenId, attributeValues, new bytes32[](0), new address[](0), new bytes[](0));
    }

    function _setBytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 attributeValue) internal {
        bytes32[] memory attributeNames = new bytes32[](1);
        attributeNames[0] = attributeName;
        bytes32[] memory attributeValues = new bytes32[](1);
        attributeValues[0] = attributeValue;

        _setAttributes(attributeNames, tokenId, new uint256[](0), attributeValues, new address[](0), new bytes[](0));
    }

    function _setBytes32Attributes(bytes32[] memory attributeNames, uint256 tokenId, bytes32[] memory attributeValues) internal {
        _setAttributes(attributeNames, tokenId, new uint256[](0), attributeValues, new address[](0), new bytes[](0));
    }

    function _setAddressAttribute(bytes32 attributeName, uint256 tokenId, address attributeValue) internal {
        bytes32[] memory attributeNames = new bytes32[](1);
        attributeNames[0] = attributeName;
        address[] memory attributeValues = new address[](1);
        attributeValues[0] = attributeValue;

        _setAttributes(attributeNames, tokenId, new uint256[](0), new bytes32[](0), attributeValues, new bytes[](0));
    }

    function _setAddressAttributes(bytes32[] memory attributeNames, uint256 tokenId, address[] memory attributeValues) internal {
        _setAttributes(attributeNames, tokenId, new uint256[](0), new bytes32[](0), attributeValues, new bytes[](0));

    }

    function _setBytesAttribute(bytes32 attributeName, uint256 tokenId, bytes memory attributeValue) internal {
        bytes32[] memory attributeNames = new bytes32[](1);
        attributeNames[0] = attributeName;
        bytes[] memory attributeValues = new bytes[](1);
        attributeValues[0] = attributeValue;

        _setAttributes(attributeNames, tokenId, new uint256[](0), new bytes32[](0), new address[](0), attributeValues);

    }

    function _setBytesAttributes(bytes32[] memory attributeNames, uint256 tokenId, bytes[] memory attributeValues) internal {
        _setAttributes(attributeNames, tokenId, new uint256[](0), new bytes32[](0), new address[](0), attributeValues);
    }

    function _setAttributes(
        bytes32[] memory attributeNames,
        uint256 tokenId,
        uint256[] memory uint256AttributeValues,
        bytes32[] memory bytes32AttributeValues,
        address[] memory addressAttributeValues,
        bytes[] memory bytesAttributeValues
    ) internal {

        require(
            attributeNames.length ==
            uint256AttributeValues.length +
            bytes32AttributeValues.length +
            addressAttributeValues.length +
            bytesAttributeValues.length,
            "721 setAttributes, length mismatch"
        );

        uint256 attributeNameIndex = 0;

        for (uint256 i = 0; i < uint256AttributeValues.length; i++) {
            _fixedAttribute[attributeNames[attributeNameIndex]][tokenId] = bytes32(uint256AttributeValues[i]);
            attributeNameIndex++;
        }

        for (uint256 i = 0; i < bytes32AttributeValues.length; i++) {
            _fixedAttribute[attributeNames[attributeNameIndex]][tokenId] = bytes32AttributeValues[i];
            attributeNameIndex++;
        }

        for (uint256 i = 0; i < addressAttributeValues.length; i++) {
            _fixedAttribute[attributeNames[attributeNameIndex]][tokenId] = bytes32(uint256(uint160(addressAttributeValues[i])));
            attributeNameIndex++;
        }

        for (uint256 i = 0; i < bytesAttributeValues.length; i++) {
            _dynamicAttribute[attributeNames[attributeNameIndex]][tokenId] = bytesAttributeValues[i];
            attributeNameIndex++;
        }

        emit allAttribute(
            attributeNames,
            tokenId,
            msg.sender,
            uint256AttributeValues,
            bytes32AttributeValues,
            addressAttributeValues,
            bytesAttributeValues
        );
    }


    function _doLock(uint256 tokenId, address unlocker) internal {
        _tokenLocks[tokenId] = _tokenLocks[tokenId].add(1);
        _tokenLockCounts[tokenId][unlocker] = _tokenLockCounts[tokenId][unlocker].add(1);
    }

    //unlock more times should be fine
    function _doUnlock(uint256 tokenId, address unlocker) internal {
        if (_tokenLockCounts[tokenId][unlocker] == 0) {
            return;
        }
        _tokenLocks[tokenId] = _tokenLocks[tokenId].sub(1);
        _tokenLockCounts[tokenId][unlocker] = _tokenLockCounts[tokenId][unlocker].sub(1);
    }

    //==================================

    function setTokenIdMapRange(uint256 begin, uint256 end) override external onlyOwner {
        require(begin < end, "setTokenIdMapRange, begin < end ?");
        _tokenIdMapRangeBegin = begin;
        _tokenIdMapRangeEnd = end;
    }

    function setTokenIdCounter(uint256 newTokenIdCounter, bool needSub1) override external onlyOwner {
        if (needSub1) {
            newTokenIdCounter = newTokenIdCounter.sub(1);
        }
        _tokenIdCounter._value = newTokenIdCounter;
    }

    function setAccessControl(address accessControl_) override external onlyOwner {
        _setAccessControl(accessControl_);
        emit SetAccessControl(accessControl_);
    }

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) override external onlyOwner {
        _supportTransfer = supportTransfer_;
        _supportMint = supportMint_;
        _supportBurn = supportBurn_;
        emit SetSupport(supportTransfer_, supportMint_, supportBurn_);
    }

    function setBlockListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _blockListFrom[from[i]] = flag;
        }
    }

    function setBlockListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _blockListTo[to[i]] = flag;
        }
    }

    function setPrivilegeListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _privilegeListFrom[from[i]] = flag;
        }
    }

    function setPrivilegeListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _privilegeListTo[to[i]] = flag;
        }
    }

    function setBaseURI(string memory baseURI_) override external onlyOwner {
        _setBaseURI(baseURI_);
    }

    function setAttributeRegistries(
        HERC721IMType.AttributeRegistry[] memory adds,
        HERC721IMType.AttributeRegistry[] memory removes
    ) override external onlyOwner {
        for (uint256 i = 0; i < adds.length; i++) {
            if (!_attributeNames.contains(adds[i].attributeName)) {
                _attributeNames.add(adds[i].attributeName);
            }
            _attributeType[adds[i].attributeName] = adds[i].attributeType;
        }

        for (uint256 i = 0; i < removes.length; i++) {
            if (_attributeNames.contains(removes[i].attributeName)) {
                _attributeNames.remove(removes[i].attributeName);
            }
            _attributeType[removes[i].attributeName] = HERC721IMType.ATTRIBUTE_TYPE_UNKNOWN;
        }
    }
    //==========

    function tokenIdMapRange() view external returns (uint256, uint256){
        return (_tokenIdMapRangeBegin, _tokenIdMapRangeEnd);
    }

    function frozen(uint256 tokenId) override view external returns (uint256){
        return _tokenLocks[tokenId];
    }

    function frozenDetail(uint256 tokenId, address unlock) override view external returns (uint256){
        return _tokenLockCounts[tokenId][unlock];
    }

    function exists(uint256 tokenId) override view external returns (bool) {
        return _exists(tokenId);
    }

    //warning, gasLimit, memoryLimit
    function tokensOfOwner(address owner) override view external returns (uint256[] memory) {
        return _holderTokens[owner].values();
    }

    function listAttributeRegistries() override view external returns (HERC721IMType.AttributeRegistry[] memory){
        HERC721IMType.AttributeRegistry[] memory ret = new HERC721IMType.AttributeRegistry[](_attributeNames.length());
        for (uint256 i = 0; i < ret.length; i++) {
            ret[i].attributeName = _attributeNames.at(i);
            ret[i].attributeType = _attributeType[ret[i].attributeName];
        }
        return ret;
    }

    function getUint256Attribute(bytes32 attributeName, uint256 tokenId) override view external returns (uint256) {
        return uint256(_fixedAttribute[attributeName][tokenId]);
    }

    function getUint256Attributes(bytes32[] memory attributeName, uint256 tokenId) override view external returns (uint256[] memory) {
        uint256[] memory ret = new uint256[](attributeName.length);
        for (uint256 i = 0; i < attributeName.length; i++) {
            ret[i] = uint256(_fixedAttribute[attributeName[i]][tokenId]);
        }
        return ret;
    }

    function getBytes32Attribute(bytes32 attributeName, uint256 tokenId) override view external returns (bytes32) {
        return _fixedAttribute[attributeName][tokenId];
    }

    function getBytes32Attributes(bytes32[] memory attributeName, uint256 tokenId) override view external returns (bytes32[] memory) {
        bytes32[] memory ret = new bytes32[](attributeName.length);
        for (uint256 i = 0; i < attributeName.length; i++) {
            ret[i] = _fixedAttribute[attributeName[i]][tokenId];
        }
        return ret;
    }

    function getAddressAttribute(bytes32 attributeName, uint256 tokenId) override view external returns (address)  {
        return address(uint160(uint256(_fixedAttribute[attributeName][tokenId])));
    }

    function getAddressAttributes(bytes32[] memory attributeName, uint256 tokenId) override view external returns (address[] memory) {
        address[] memory ret = new address[](attributeName.length);
        for (uint256 i = 0; i < attributeName.length; i++) {
            ret[i] = address(uint160(uint256(_fixedAttribute[attributeName[i]][tokenId])));
        }
        return ret;
    }

    function getBytesAttribute(bytes32 attributeName, uint256 tokenId) override view external returns (bytes memory)  {
        return _dynamicAttribute[attributeName][tokenId];
    }

    function getBytesAttributes(bytes32[] memory attributeName, uint256 tokenId) override view external returns (bytes[] memory) {
        bytes[] memory ret = new bytes[](attributeName.length);
        for (uint256 i = 0; i < attributeName.length; i++) {
            ret[i] = _dynamicAttribute[attributeName[i]][tokenId];
        }
        return ret;
    }

    function support() override view external returns (bool supportTransfer, bool supportMint, bool supportBurn){
        return (_supportTransfer, _supportMint, _supportBurn);
    }

    function transferTxs() override view external returns (uint256) {
        return _transferTxs;
    }

    function interactAccountsLength() override view external returns (uint256){
        return _interactAccounts.length();
    }

    function interactAccountsContains(address who) override view external returns (bool){
        return _interactAccounts.contains(who);
    }

    function interactedAccountsAt(uint256 index) override view external returns (address){
        return _interactAccounts.at(index);
    }

    function _beforeTokenTransfer(address from, address to, uint256 tokenId) virtual override(HERC721Logic) internal {
        require(tokenId != uint256(0), "tokenId can not be zero");
        require(_tokenLocks[tokenId] == 0, "can not transfer locked token");

        NameServiceInterface ns = nsUnsafe();

        if (from == address(0) && to != address(0)) {
            //mint mode
            //some how you call _mint :) , you must have done job of mint permission
            _beforeTokenTransferMint(to, tokenId, address(ns));

        } else if (from != address(0) && to == address(0)) {
            //burn mode
            //some how you call _burn :) , you must have done job of burn permission
            _beforeTokenTransferBurn(from, tokenId, address(ns));
        } else if (from != address(0) && to != address(0)) {
            //transfer
            _beforeTokenTransferTransfer(from, to, tokenId, address(ns));
        } else {
            revert("transfer, 'from' and 'to' should never be address(0) at same time");
        }

        //impossible to hit the 2^256 :)
    unchecked{_transferTxs = _transferTxs + 1;}

        if (from != address(0)) {
            _interactAccounts.add(from);
        }
        if (to != address(0)) {
            _interactAccounts.add(to);
        }

        HERC721Logic._beforeTokenTransfer(from, to, tokenId);
    }

    function _beforeTokenTransferMint(address to, uint256, address ns) view internal {

        //not support mint
        if (_sudoMint) {
            //pass
        } else {
            if (address(ns) != address(0) && NameServiceInterface(ns).isPrivileged(to)) {
                //privilege pass
            } else if (_privilegeListTo[to]) {
                //privilege pass
            } else if (address(ns) != address(0) && NameServiceInterface(ns).isBlocked(to)) {
                revert("ac.isBlocked-to");
            } else if (_blockListTo[to]) {
                revert("blockListTo");
            } else {
                if (_supportMint) {

                } else {
                    revert("un-support mint");
                }
            }
        }
    }

    function _beforeTokenTransferBurn(address from, uint256, address ns) view internal {

        if (_sudoBurn) {
            //pass
        } else {
            if (address(ns) != address(0) && NameServiceInterface(ns).isPrivileged(from)) {
                //privilege overrides blockList
            } else if (_privilegeListFrom[from]) {
                //privilege overrides blockList
            } else if (address(ns) != address(0) && NameServiceInterface(ns).isBlocked(from)) {
                revert("ac.isBlocked-from");
            } else if (_blockListFrom[from]) {
                revert("blockListFrom");
            } else {
                if (_supportBurn) {

                } else {
                    revert("un-support burn");
                }
            }
        }
    }

    function _beforeTokenTransferTransfer(address from, address to, uint256, address ns) view internal {

        if (address(ns) != address(0) &&
            (NameServiceInterface(ns).isPrivileged(from) || NameServiceInterface(ns).isPrivileged(to))
        ) {
            //privilege overrides blockList
        } else if (_privilegeListFrom[from] || _privilegeListTo[to]) {
            //privilege overrides blockList
        } else if (address(ns) != address(0) &&
            (NameServiceInterface(ns).isBlocked(from) || NameServiceInterface(ns).isBlocked(to))
        ) {
            revert("ac.isBlocked-from||ac.isBlocked-to");
        } else if (_blockListFrom[from] || _blockListTo[to]) {
            revert("blockListFrom||blockListTo");
        } else {
            if (_supportTransfer) {

            } else {
                revert("un-support transfer");
            }
        }
    }
}

